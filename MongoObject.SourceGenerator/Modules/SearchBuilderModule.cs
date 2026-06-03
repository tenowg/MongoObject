using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    /// <summary>
    /// Generates fluent search builder classes for document monitors.
    /// Creates a base search builder and projection-specific builders.
    /// </summary>
    internal class SearchBuilderModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            var ct = context.CancellationToken;

            // Generate base search builder
            GenerateBaseSearchBuilder(context, model, ct);

            // Generate projection-specific builders
            foreach (var projection in model.Projections)
            {
                ct.ThrowIfCancellationRequested();
                GenerateProjectionSearchBuilder(context, model, projection, ct);
            }
        }

        private static void GenerateBaseSearchBuilder(SourceProductionContext context, CommonModel model, System.Threading.CancellationToken ct)
        {
            var sb = new StringBuilder(4096);

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Fluent search builder for {model.Name} documents.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {model.Name}SearchBuilder");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> _monitor;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Name}Query>? _query;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query>? _meta;");
            sb.AppendLine($"        private int _limit;");
            sb.AppendLine($"        private int _skip;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {model.Name}SearchBuilder(global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor)");
            sb.AppendLine("        {");
            sb.AppendLine("            _monitor = monitor;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Internal constructor for copying state
            sb.AppendLine($"        internal {model.Name}SearchBuilder(");
            sb.AppendLine($"            global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            global::System.Action<global::{model.Namespace}.{model.Name}Query>? query,");
            sb.AppendLine($"            global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query>? meta,");
            sb.AppendLine($"            int limit,");
            sb.AppendLine($"            int skip)");
            sb.AppendLine("        {");
            sb.AppendLine("            _monitor = monitor;");
            sb.AppendLine("            _query = query;");
            sb.AppendLine("            _meta = meta;");
            sb.AppendLine("            _limit = limit;");
            sb.AppendLine("            _skip = skip;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithLimit method
            sb.AppendLine($"        public {model.Name}SearchBuilder WithLimit(int limit)");
            sb.AppendLine("        {");
            sb.AppendLine("            _limit = limit;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithSkip method
            sb.AppendLine($"        public {model.Name}SearchBuilder WithSkip(int skip)");
            sb.AppendLine("        {");
            sb.AppendLine("            _skip = skip;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithQuery method
            sb.AppendLine($"        public {model.Name}SearchBuilder WithQuery(global::System.Action<global::{model.Namespace}.{model.Name}Query> query)");
            sb.AppendLine("        {");
            sb.AppendLine("            _query = query;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithMeta method
            sb.AppendLine($"        public {model.Name}SearchBuilder WithMeta(global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query> meta)");
            sb.AppendLine("        {");
            sb.AppendLine("            _meta = meta;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Projection methods
            foreach (var projection in model.Projections)
            {
                ct.ThrowIfCancellationRequested();
                sb.AppendLine($"        public {model.Name}{projection.Name}SearchBuilder With{projection.Name}Projection()");
                sb.AppendLine("        {");
                sb.AppendLine($"            return new {model.Name}{projection.Name}SearchBuilder(_monitor, _query, _meta, _limit, _skip);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // GetAwaiter method
            sb.AppendLine($"        public global::System.Runtime.CompilerServices.TaskAwaiter<global::System.Collections.Generic.IEnumerable<global::{model.Namespace}.{model.Name}>> GetAwaiter()");
            sb.AppendLine("        {");
            sb.AppendLine("            return ExecuteAsync().GetAwaiter();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // ExecuteAsync method
            sb.AppendLine($"        private async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::{model.Namespace}.{model.Name}>> ExecuteAsync()");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (_monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return await internalMonitor.CombinedSearch<global::{model.Namespace}.{model.Name}Query, global::{model.Namespace}.{model.Metadata.Name}Query>(_query, _meta, _limit, _skip);");
            sb.AppendLine("            }");
            sb.AppendLine($"            return global::System.Array.Empty<global::{model.Namespace}.{model.Name}>();");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.SearchBuilder.g.cs", sb.ToString());
        }

        private static void GenerateProjectionSearchBuilder(SourceProductionContext context, CommonModel model, ProjectionModel projection, System.Threading.CancellationToken ct)
        {
            var sb = new StringBuilder(4096);
            var projectionTypeName = $"{model.Name}{projection.Name}";

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Fluent search builder for {model.Name} documents with {projection.Name} projection.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {projectionTypeName}SearchBuilder");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> _monitor;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Name}Query>? _query;");
            sb.AppendLine($"        private global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query>? _meta;");
            sb.AppendLine($"        private int _limit;");
            sb.AppendLine($"        private int _skip;");
            sb.AppendLine();

            // Internal constructor
            sb.AppendLine($"        internal {projectionTypeName}SearchBuilder(");
            sb.AppendLine($"            global::MongoObject.Core.Interfaces.IDocumentMonitor<global::{model.Namespace}.{model.Name}> monitor,");
            sb.AppendLine($"            global::System.Action<global::{model.Namespace}.{model.Name}Query>? query,");
            sb.AppendLine($"            global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query>? meta,");
            sb.AppendLine($"            int limit = 0,");
            sb.AppendLine($"            int skip = 0)");
            sb.AppendLine("        {");
            sb.AppendLine("            _monitor = monitor;");
            sb.AppendLine("            _query = query;");
            sb.AppendLine("            _meta = meta;");
            sb.AppendLine("            _limit = limit;");
            sb.AppendLine("            _skip = skip;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithQuery method
            sb.AppendLine($"        public {projectionTypeName}SearchBuilder WithQuery(global::System.Action<global::{model.Namespace}.{model.Name}Query> query)");
            sb.AppendLine("        {");
            sb.AppendLine("            _query = query;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithLimit method
            sb.AppendLine($"        public {projectionTypeName}SearchBuilder WithLimit(int limit)");
            sb.AppendLine("        {");
            sb.AppendLine("            _limit = limit;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithSkip method
            sb.AppendLine($"        public {projectionTypeName}SearchBuilder WithSkip(int skip)");
            sb.AppendLine("        {");
            sb.AppendLine("            _skip = skip;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // WithMeta method
            sb.AppendLine($"        public {projectionTypeName}SearchBuilder WithMeta(global::System.Action<global::{model.Namespace}.{model.Metadata.Name}Query> meta)");
            sb.AppendLine("        {");
            sb.AppendLine("            _meta = meta;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // GetAwaiter method - returns projected type
            sb.AppendLine($"        public global::System.Runtime.CompilerServices.TaskAwaiter<global::System.Collections.Generic.IEnumerable<global::{model.Namespace}.{projectionTypeName}>> GetAwaiter()");
            sb.AppendLine("        {");
            sb.AppendLine("            return ExecuteAsync().GetAwaiter();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // ExecuteAsync method with projection
            sb.AppendLine($"        private async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::{model.Namespace}.{projectionTypeName}>> ExecuteAsync()");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (_monitor is global::MongoObject.Core.Interfaces.IDocumentMonitorInternal<global::{model.Namespace}.{model.Name}> internalMonitor)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return await internalMonitor.SearchWithProjection<global::{model.Namespace}.{model.Name}Query, global::{model.Namespace}.{model.Metadata.Name}Query, global::{model.Namespace}.{projectionTypeName}>(_query, _meta, _limit, _skip);");
            sb.AppendLine("            }");
            sb.AppendLine($"            return global::System.Array.Empty<global::{model.Namespace}.{projectionTypeName}>();");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.{projection.Name}SearchBuilder.g.cs", sb.ToString());
        }
    }
}