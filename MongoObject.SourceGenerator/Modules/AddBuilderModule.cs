using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Helpers;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;

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
            var isb = new IndentedStringBuilder();

            isb.AppendLine("#nullable enable");
            isb.AppendLine("// auto-generated");
            isb.AppendLine($"namespace {model.Namespace}");
            using (isb.Block())
            {
                isb.AppendLine($"/// <summary>");
                isb.AppendLine($"/// Fluent add builder for {model.Name} documents.");
                isb.AppendLine($"/// </summary>");
                isb.AppendLine($"public class {model.Name}AddBuilder");
                using (isb.Block())
                {
                    isb.AppendLine($"private readonly global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> _monitor;");
                    isb.AppendLine($"private readonly global::{model.Namespace}.{model.Name} _document;");
                    isb.AppendLine($"private global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Record>? _meta;");
                    isb.AppendLine();

                    // Constructor
                    isb.AppendLine($"public {model.Name}AddBuilder(");
                    isb.AppendLine($"    global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
                    isb.AppendLine($"    global::{model.Namespace}.{model.Name} document)");
                    using (isb.Block())
                    {
                        isb.AppendLine("_monitor = monitor;");
                        isb.AppendLine("_document = document;");
                    }

                    // WithMeta method
                    isb.AppendLine($"public {model.Name}AddBuilder WithMeta(global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Record> meta)");
                    using (isb.Block())
                    {
                        isb.AppendLine("_meta = meta;");
                        isb.AppendLine("return this;");
                    }
                    // GetAwaiter method
                    isb.AppendLine("public global::System.Runtime.CompilerServices.TaskAwaiter<string> GetAwaiter()");
                    using (isb.Block())
                    {
                        isb.AppendLine("return ExecuteAsync().GetAwaiter();");
                    }
                    // ExecuteAsync method
                    isb.AppendLine("private async global::System.Threading.Tasks.Task<string> ExecuteAsync()");
                    using (isb.Block())
                    {
                        isb.AppendLine($"if (_monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
                        using (isb.Block())
                        {    
                            isb.AppendLine($"return await internalMonitor.Add<global::{model.Namespace}.{model.Metadata.Name}Record>(_document, _meta);");
                        }
                        isb.AppendLine("return string.Empty;");
                    }
                }
            }

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.AddBuilder.g.cs", isb.ToString());
        }
    }
}