using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Helpers;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Collections.Immutable;
using System.Linq;

namespace MongoObject.SourceGenerator.Modules
{
    /// <summary>
    /// Generates the DI registration extension method that registers all discovered document types.
    /// And builds the BsonDocument used to build the schema in the Cli Tool
    /// </summary>
    internal class ObjectDiscoveryModule : ICodeModuleMultiple
    {
        public void Execute(SourceProductionContext context, (ImmutableArray<CommonModel?> models, string rootNamespace) args)
        {
            var (models, rootNamespace) = args;
            var encryptedModels = models.Where(x  => (x != null && x.IsEncryptedModel)).ToList();
            if (models.Length == 0) return;

            var sb = new IndentedStringBuilder();

            sb.AppendLine("// auto-generated");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine();
            sb.AppendLine($"namespace {rootNamespace}.Extensions");
            using (sb.Block())
            {
                sb.AppendLine("internal static class ObjectDiscovery");
                using (sb.Block())
                {
                    sb.AppendLine("extension(global::MongoObject.Core.Extensions.MongoObjectBuilder builder)");
                    using (sb.Block())
                    {
                        sb.AppendLine($"public global::MongoObject.Core.Extensions.MongoObjectBuilder RegisterDocuments{SanitizeName(rootNamespace)}()");
                        using (sb.Block())
                        {
                            sb.AppendLine($"builder.RegisterIndexBuilder<global::{args.rootNamespace}.{args.rootNamespace.Replace(".", "")}IndexBuilder>();");
                            foreach (var model in models)
                            {
                                if (model == null) continue;

                                sb.AppendLine($"builder.RegisterDocument<global::{model.Namespace}.{model.Name}, global::{model.Namespace}.{model.Metadata.Name}Query, global::{model.Namespace}.{model.Metadata.Name}Record>({model.IsEncryptedModel.ToString().ToLowerInvariant()});");
                            }

                            foreach (var model in encryptedModels)
                            {
                                sb.AppendLine($"builder.Services.AddSingleton<global::MongoObject.Core.Interfaces.IEncryptionBuilder, global::{model!.Namespace}.{model!.Name}EncryptionBuilder>();");
                            }

                            sb.AppendLine("return builder;");
                        }
                    }
                    sb.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                    sb.AppendLine("public static void Initialize()");
                    using (sb.Block())
                    {
                        sb.AppendLine("var payload = new global::MongoDB.Bson.BsonDocument();");
                        sb.AppendLine("if (global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument.TryGetValue(\"documentSchema\", out var document))");
                        using(sb.Block())
                        {
                            sb.AppendLine("payload = document.AsBsonDocument;");
                        }
                        foreach (var model in models)
                        {
                            sb.AppendLine($"var {model!.Name}Document = new global::MongoDB.Bson.BsonDocument");
                            using (sb.Block(closer: ";"))
                            {
                                using (sb.Block(closer: ","))
                                {
                                    sb.AppendLine("\"properties\", new global::MongoDB.Bson.BsonArray");
                                    using (sb.Block())
                                    {
                                        foreach (var prop in model.Properties)
                                        {
                                            sb.AppendLine($"new global::MongoDB.Bson.BsonDocument");
                                            using (sb.Block(closer: ","))
                                            {
                                                sb.AppendLine($"{{\"name\", \"{prop.Name}\"}},");
                                                sb.AppendLine($"{{\"type_name\", \"{(prop.FullName.StartsWith("global::") ? prop.FullName.Substring("global::".Length) : prop.FullName)}\"}},");
                                                sb.AppendLine($"{{\"queryName\", \"{prop.QueryName}\"}},");
                                                sb.AppendLine($"{{\"isEncrypted\", {prop.isEncrypted.ToString().ToLowerInvariant()}}},");
                                                sb.AppendLine($"{{\"bson_type\", \"{prop.BsonType}\"}},");
                                                sb.AppendLine($"{{\"is_required\", {prop.IsRequired.ToString().ToLowerInvariant()}}}");
                                            }
                                        }
                                    }
                                }
                                sb.AppendLine($"{{\"name\", \"{model.Name}\"}},");
                                sb.AppendLine($"{{\"is_encrypted\", {model.IsEncryptedModel.ToString().ToLowerInvariant()}}},");
                                sb.AppendLine($"{{\"collection_name\", \"{model.CollectionName}\"}},");
                                sb.AppendLine($"{{\"database_name\", \"{model.DatabaseName}\"}},");
                                sb.AppendLine($"{{\"bson_type\", \"object\"}},");
                                sb.AppendLine($"{{\"type_name\", \"{model.Namespace}.{model.Name}\"}},");
                                sb.AppendLine($"{{\"migration_policy\", \"{model.MigrationPolicy}\"}}");
                            }
                            sb.AppendLine($"payload.Add(\"{model.Name}\", {model.Name}Document);");
                        }
                        sb.AppendLine($"global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument[\"documentSchema\"] = payload;");
                        sb.AppendLine($"global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument[\"base_namespace\"] = \"{rootNamespace}\";");
                    }
                }
            }

            // Use a unique file name that won't conflict with ExtensionModule
            context.AddSource($"{rootNamespace.Replace(".", "_")}_ObjectDiscovery.g.cs", sb.ToString());
        }

        private static string SanitizeName(string name)
        {
            return name.Replace(".", "_").Replace("-", "_");
        }
    }
}
