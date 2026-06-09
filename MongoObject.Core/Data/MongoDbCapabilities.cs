namespace MongoObject.Core.Data
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Linq;
    using System.Threading.Tasks;

    // Explicit Feature Flags
    public record MongoServerCapabilities(int MaxWireVersion, bool IsEnterprise, bool IsAtlasEnvironment, bool SupportsVectorSearch)
    {
        public bool SupportsWindowFunctions => MaxWireVersion >= 13; // MongoDB 5.0+

        public static async Task<MongoServerCapabilities> ResolveAsync(IMongoClient client)
        {
            var adminDb = client.GetDatabase("admin");

            // 1. Get Wire Version
            var helloCmd = await adminDb.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
            int wireVersion = helloCmd.GetValue("maxWireVersion", 0).AsInt32;

            // 2. Get Edition (Community vs Enterprise)
            var buildInfo = await adminDb.RunCommandAsync<BsonDocument>(new BsonDocument("buildInfo", 1));
            var modules = buildInfo.GetValue("modules", new BsonArray()).AsBsonArray.Select(x => x.AsString);
            bool isEnterprise = modules.Contains("enterprise");

            // 3. Check for Atlas network topology
            bool isAtlas = client.Settings.Servers.Any(s => s.Host.EndsWith(".mongodb.net"));

            // 4. Probe for Vector Search capability
            bool supportsVectorSearch = await ProbeForPipelineStageAsync(adminDb, "$vectorSearch");

            return new MongoServerCapabilities(wireVersion, isEnterprise, isAtlas, supportsVectorSearch);
        }

        private static async Task<bool> ProbeForPipelineStageAsync(IMongoDatabase db, string stageName)
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

                await db.RunCommandAsync<BsonDocument>(BsonDocument.Parse(command));
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
