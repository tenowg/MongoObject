using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Collections.Immutable;
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
            if (models.Length == 0) return;

            var sb = new StringBuilder(2048);

            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {rootNamespace}.Extensions");
            sb.AppendLine("{");
            sb.AppendLine("    internal static class ObjectDiscovery");
            sb.AppendLine("    {");
            sb.AppendLine("        extension(global::MongoObject.Core.Extensions.MongoObjectBuilder builder)");
            sb.AppendLine("        {");
            sb.AppendLine($"            public global::MongoObject.Core.Extensions.MongoObjectBuilder RegisterDocuments{SanitizeName(rootNamespace)}()");
            sb.AppendLine("            {");

            foreach (var model in models)
            {
                if (model == null) continue;

                sb.AppendLine($"                builder.RegisterDocument<global::{model.Namespace}.{model.Name}, global::{model.Namespace}.{model.Metadata.Name}Query, global::{model.Namespace}.{model.Metadata.Name}Record>();");
            }

            sb.AppendLine("                return builder;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Use a unique file name that won't conflict with ExtensionModule
            context.AddSource($"{rootNamespace.Replace(".", "_")}_ObjectDiscovery.g.cs", sb.ToString());
        }

        private static string SanitizeName(string name)
        {
            return name.Replace(".", "_").Replace("-", "_");
        }
    }
}
