using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    /// <summary>
    /// Generates fluent add builder classes for document monitors.
    /// </summary>
    internal class AddBuilderModule : ICodeModule
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
            sb.AppendLine($"    /// Fluent add builder for {model.Name} documents.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {model.Name}AddBuilder");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> _monitor;");
            sb.AppendLine($"        private readonly global::{model.Namespace}.{model.Name} _document;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Record>? _meta;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {model.Name}AddBuilder(");
            sb.AppendLine($"            global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            global::{model.Namespace}.{model.Name} document)");
            sb.AppendLine("        {");
            sb.AppendLine("            _monitor = monitor;");
            sb.AppendLine("            _document = document;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithMeta method
            sb.AppendLine($"        public {model.Name}AddBuilder WithMeta(global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Record> meta)");
            sb.AppendLine("        {");
            sb.AppendLine("            _meta = meta;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // GetAwaiter method
            sb.AppendLine("        public global::System.Runtime.CompilerServices.TaskAwaiter<string> GetAwaiter()");
            sb.AppendLine("        {");
            sb.AppendLine("            return ExecuteAsync().GetAwaiter();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // ExecuteAsync method
            sb.AppendLine("        private async global::System.Threading.Tasks.Task<string> ExecuteAsync()");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (_monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return await internalMonitor.Add<global::{model.Namespace}.{model.Metadata.Name}Record>(_document, _meta);");
            sb.AppendLine("            }");
            sb.AppendLine("            return string.Empty;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.AddBuilder.g.cs", sb.ToString());
        }
    }
}