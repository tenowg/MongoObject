using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Encryption.Helpers;
using MongoObject.SourceGenerator.Encryption.Interfaces;
using MongoObject.SourceGenerator.Encryption.Models;
using System.Collections.Immutable;
using System.Linq;

namespace MongoObject.SourceGenerator.Encryption.Modules
{
    internal class ObjectBuilderModule : ICodeModuleMultiple
    {
        public void Execute(SourceProductionContext context, (ImmutableArray<(CommonModel?, EncryptedClassModel?)> models, string rootNamespace) args)
        {
            var rootNamespace = args.rootNamespace;
            var models = args.models.Where(x => x.Item2 != null).Select(x => x.Item2);
            var sb = new IndentedStringBuilder();

            // need to get the pluginBsonObjectRegistery
            // check if the object exists, if it does add the additional data, if not build a skeleton the base generator will fill
            // this will be a ModuleInitializer
            sb.AppendLine("using System.Linq;");
            sb.AppendLine();
            sb.AppendLine($"namespace {rootNamespace}.Extensions");
            using (sb.Block())
            {
                sb.AppendLine("public class ObjectInitializer");
                using (sb.Block())
                {
                    sb.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                    sb.AppendLine("public static void Initialize()");
                    using (sb.Block())
                    {
                        sb.AppendLine("var payload = new global::MongoDB.Bson.BsonDocument();");
                        sb.AppendLine("if (global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument.TryGetValue(\"documentSchema\", out var document))");
                        using (sb.Block())
                        {
                            sb.AppendLine("payload = document.AsBsonDocument;");
                        }

                        foreach (var model in models)
                        {
                            sb.AppendLine($"if(payload.TryGetValue(\"{model!.Name}\", out var {model.Name}ValueDocument))");

                            using (sb.Block())
                            {
                                sb.AppendLine($"var {model.Name}Document = {model.Name}ValueDocument.AsBsonDocument;");
                                sb.AppendLine($"{model.Name}Document[\"encryption_key\"] = \"{model.ProviderKey}\";");
                                sb.AppendLine($"if({model.Name}Document.TryGetValue(\"properties\", out var propertiesValue))");

                                using (sb.Block())
                                {
                                    sb.AppendLine($"var properties = propertiesValue.AsBsonArray;");
                                    foreach (var prop in model.Properties)
                                    {
                                        sb.AppendLine($"var Property{prop.Name}Document = properties");
                                        sb.AppendLine(".Select(item => item.AsBsonDocument)");
                                        sb.AppendLine($".FirstOrDefault(doc => doc.Contains(\"name\") && doc[\"name\"] == \"{prop.Name}\");");
                                        sb.AppendLine($"if (Property{prop.Name}Document != null)");
                                        using (sb.Block())
                                        {
                                            sb.AppendLine($"Property{prop.Name}Document[\"isEncrypted\"] = {prop.IsEncrypted.ToString().ToLowerInvariant()};");
                                        }
                                    }
                                }
                            }
                            sb.AppendLine("else");
                            using (sb.Block())
                            {
                                sb.AppendLine($"var {model!.Name}AddDocument = new global::MongoDB.Bson.BsonDocument");
                                using (sb.Block(closer: ";"))
                                {
                                    sb.AppendLine($"{{\"is_encrypted\", true}},");
                                    sb.AppendLine($"{{\"encryption_key\", \"{model.ProviderKey}\"}},");
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
                                                    sb.AppendLine($"{{\"isEncrypted\", {prop.IsEncrypted.ToString().ToLowerInvariant()}}}");
                                                }
                                            }
                                        }
                                    }

                                }
                                sb.AppendLine($"payload.Add(\"{model.Name}\", {model.Name}AddDocument);");
                            }
                            sb.AppendLine($"global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument[\"documentSchema\"] = payload;");
                        }
                    }
                }
            }
            context.AddSource($"{args.rootNamespace.Replace(".", "_")}_EncryptionObjectBuilder.g.cs", sb.ToString());
        }
    }
}
