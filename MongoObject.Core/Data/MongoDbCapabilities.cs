namespace MongoObject.Core.Data
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.Core.Clusters;
    using System.Linq;

    // Explicit Feature Flags
    internal sealed record MongoServerCapabilities(int MaxWireVersion, bool IsEnterprise, bool IsAtlasEnvironment, bool SupportsVectorSearch, ClusterType ClusterType)
    {
        public bool SupportsWindowFunctions => MaxWireVersion >= 13; // MongoDB 5.0+

        public static MongoServerCapabilities Resolve(IMongoClient client)
        {
            var adminDb = client.GetDatabase("admin");

            // 1. Get Wire Version
            var helloCmd = adminDb.RunCommand<BsonDocument>(new BsonDocument("hello", 1));
            int wireVersion = helloCmd.GetValue("maxWireVersion", 0).AsInt32;

            // 2. Get Edition (Community vs Enterprise)
            var buildInfo = adminDb.RunCommand<BsonDocument>(new BsonDocument("buildInfo", 1));
            var modules = buildInfo.GetValue("modules", new BsonArray()).AsBsonArray.Select(x => x.AsString);
            bool isEnterprise = modules.Contains("enterprise");

            // 3. Check for Atlas network topology
            bool isAtlas = client.Settings.Servers.Any(s => s.Host.EndsWith(".mongodb.net"));

            // 4. Probe for Vector Search capability
            bool supportsVectorSearch = ProbeForPipelineStage(adminDb, "$vectorSearch");

            var clusterType = client.Cluster.Description.Type;

            return new MongoServerCapabilities(wireVersion, isEnterprise, isAtlas, supportsVectorSearch, clusterType);
        }

        private static bool ProbeForPipelineStage(IMongoDatabase db, string stageName)
        {
            try
            {
                // The dummy stage payload doesn't matter, we just need the parser to read the key
                string command = $@"{{ 
                explain: {{ 
                    aggregate: 'system.version', 
                    pipeline: [{{ {stageName}: {{}} }}], 
                    cursor: {{}} 
                }} 
            }}";

                db.RunCommand<BsonDocument>(BsonDocument.Parse(command));
                return true;
            }
            catch (MongoCommandException ex) when (ex.Code == 40324 || ex.ErrorMessage.Contains("Unrecognized pipeline stage"))
            {
                return false;
            }
            catch (MongoCommandException)
            {
                // Threw a different error (e.g. invalid arguments for the stage), meaning the stage exists
                return true;
            }
        }
    }
}
