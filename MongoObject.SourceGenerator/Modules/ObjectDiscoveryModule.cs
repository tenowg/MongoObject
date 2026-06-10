using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Helpers;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    /// <summary>
    /// Generates the DI registration extension method that registers all discovered document types.
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

                            foreach(var model in encryptedModels)
                            {
                                sb.AppendLine($"builder.Services.AddSingleton<global::MongoObject.Core.Interfaces.IEncryptionBuilder, global::{model!.Namespace}.{model!.Name}EncryptionBuilder>();");
                            }

                            sb.AppendLine("return builder;");
                        }
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
