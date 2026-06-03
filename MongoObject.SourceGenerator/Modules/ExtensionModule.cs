using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    /// <summary>
    /// Generates per-class extension methods for document monitors.
    /// Extension methods are generated in the same namespace as the document class
    /// to reduce the need for extra using statements.
    /// </summary>
    internal class ExtensionModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            var sb = new StringBuilder(8192);

            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {model.Name}Extensions");
            sb.AppendLine("    {");

            // MetadataSearch extension
            GenerateMetadataSearchExtension(sb, model);

            // DocumentSearch extension
            GenerateDocumentSearchExtension(sb, model);

            // Add extension (original)
            GenerateAddExtension(sb, model);

            // Builder entry point extensions
            GenerateSearchExtension(sb, model);
            GenerateAddBuilderExtension(sb, model);
            GenerateDeleteManyExtension(sb, model);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.Extensions.g.cs", sb.ToString());
        }

        private static void GenerateMetadataSearchExtension(StringBuilder sb, CommonModel model)
        {
            sb.AppendLine("        [global::System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"        public static async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<global::{model.Namespace}.{model.Name}>> MetadataSearch(");
            sb.AppendLine($"            this global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            System.Action<global::{model.Namespace}.{model.Metadata.Name}Query> configure)");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine("                return await internalMonitor.MetadataSearch(configure);");
            sb.AppendLine("            }");
            sb.AppendLine($"            return System.Array.Empty<global::{model.Namespace}.{model.Name}>();");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void GenerateDocumentSearchExtension(StringBuilder sb, CommonModel model)
        {
            sb.AppendLine("        [global::System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"        public static async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<global::{model.Namespace}.{model.Name}>> DocumentSearch(");
            sb.AppendLine($"            this global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            System.Action<global::{model.Namespace}.{model.Name}Query> configure)");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return await internalMonitor.DocumentSearch<global::{model.Namespace}.{model.Name}Query>(configure);");
            sb.AppendLine("            }");
            sb.AppendLine($"            return System.Array.Empty<global::{model.Namespace}.{model.Name}>();");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void GenerateAddExtension(StringBuilder sb, CommonModel model)
        {
            sb.AppendLine("        [global::System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"        public static async System.Threading.Tasks.Task<string> Add(");
            sb.AppendLine($"            this global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            global::{model.Namespace}.{model.Name} document,");
            sb.AppendLine($"            System.Action<global::{model.Namespace}.{model.Metadata.Name}Record> configure)");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return await internalMonitor.Add<global::{model.Namespace}.{model.Metadata.Name}Record>(document, configure);");
            sb.AppendLine("            }");
            sb.AppendLine("            return \"Not Found\";");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void GenerateSearchExtension(StringBuilder sb, CommonModel model)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Creates a fluent search builder for {model.Name} documents.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public static global::{model.Namespace}.{model.Name}SearchBuilder Search(");
            sb.AppendLine($"            this global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new global::{model.Namespace}.{model.Name}SearchBuilder(monitor);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void GenerateAddBuilderExtension(StringBuilder sb, CommonModel model)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Creates a fluent add builder for {model.Name} documents.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public static global::{model.Namespace}.{model.Name}AddBuilder AddBuilder(");
            sb.AppendLine($"            this global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            global::{model.Namespace}.{model.Name} document)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new global::{model.Namespace}.{model.Name}AddBuilder(monitor, document);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void GenerateDeleteManyExtension(StringBuilder sb, CommonModel model)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Creates a fluent delete many builder for {model.Name} documents.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public static global::{model.Namespace}.{model.Name}DeleteManyBuilder DeleteMany(");
            sb.AppendLine($"            this global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new global::{model.Namespace}.{model.Name}DeleteManyBuilder(monitor);");
            sb.AppendLine("        }");
        }
    }
}
