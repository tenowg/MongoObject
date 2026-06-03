using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    /// <summary>
    /// Generates fluent delete many builder classes for document monitors.
    /// </summary>
    internal class DeleteManyBuilderModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            var sb = new StringBuilder(2048);

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Fluent delete many builder for {model.Name} documents.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {model.Name}DeleteManyBuilder");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> _monitor;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Name}Query>? _query;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query>? _meta;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {model.Name}DeleteManyBuilder(global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor)");
            sb.AppendLine("        {");
            sb.AppendLine("            _monitor = monitor;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithQuery method
            sb.AppendLine($"        public {model.Name}DeleteManyBuilder WithQuery(global::System.Action<global::{model.Namespace}.{model.Name}Query> query)");
            sb.AppendLine("        {");
            sb.AppendLine("            _query = query;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithMeta method
            sb.AppendLine($"        public {model.Name}DeleteManyBuilder WithMeta(global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query> meta)");
            sb.AppendLine("        {");
            sb.AppendLine("            _meta = meta;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // GetAwaiter method
            sb.AppendLine("        public global::System.Runtime.CompilerServices.TaskAwaiter<long> GetAwaiter()");
            sb.AppendLine("        {");
            sb.AppendLine("            return ExecuteAsync().GetAwaiter();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // ExecuteAsync method
            sb.AppendLine("        private async global::System.Threading.Tasks.Task<long> ExecuteAsync()");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (_monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return await internalMonitor.DeleteMany<global::{model.Namespace}.{model.Name}Query, global::{model.Namespace}.{model.Metadata.Name}Query>(_query, _meta);");
            sb.AppendLine("            }");
            sb.AppendLine("            return 0;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.DeleteManyBuilder.g.cs", sb.ToString());
        }
    }
}