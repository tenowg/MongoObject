using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;
using System.Linq;

namespace MongoObject.SourceGenerator.Modules
{
    internal class MongoObjectModule : CodeModule
    {
        public override void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            var sb = new StringBuilder(4096);

            sb.AppendLine("// auto-generated");
            sb.AppendLine();
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            if (model.BsonValidation)
            {
                sb.AppendLine($"    [global::MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]");
            }
            sb.AppendLine($"    public partial class {model.Name} : global::MongoObject.Core.Data.TrackingObservableObject,");
            sb.AppendLine($"                                         global::MongoObject.Core.Interfaces.IDocumentFile,");
            sb.AppendLine($"                                         global::MongoObject.Core.Interfaces.IDocumentFileInternal,");
            sb.AppendLine($"                                         global::MongoObject.Core.Interfaces.IDocumentFile<{model.Metadata.Name}Query, {model.Metadata.Name}Record>");
            sb.AppendLine("    {");
            sb.AppendLine("        private long _Version;");
            sb.AppendLine($"        public System.Type GetSearchMetaType() => typeof({model.Metadata.Name}Query);");
            sb.AppendLine($"        public System.Type GetRecordMetaType() => typeof({model.Metadata.Name}Record);");
            sb.AppendLine($"        public string GetDatabaseName() => \"{model.DatabaseName}\";");
            sb.AppendLine($"        public string GetCollectioName() => \"{model.CollectionName}\";");
            sb.AppendLine();
            sb.AppendLine("        [global::MongoDB.Bson.Serialization.Attributes.BsonIgnore]");
            sb.AppendLine("        public long Version { get { return _Version;} set { _Version = value; } }");

            var vectorProjections = model.Projections.Where(p => p.Properties.Any(x => x.EnumName == "Vector"));
            foreach ( var vectorProjection in vectorProjections )
            {
                sb.AppendLine($"        public float[] {vectorProjection.Name}Embedding {{ get; set; }} = [];");
            }
            //sb.AppendLine("        private readonly System.Collections.Generic.HashSet<string> _changedFields = new();");

            // Generate partial property implementations
            foreach (var prop in model.Properties)
            {
                if (prop.IsBsonIgnore)
                {
                    // For properties marked with [BsonIgnore], generate auto-properties without change tracking
                    sb.AppendLine($"        public partial {prop.FullName} {prop.Name} {{ get {{ return field; }} set {{ field = value; }} }}");
                    continue;
                }
                sb.AppendLine($"        public partial {prop.FullName} {prop.Name}");
                sb.AppendLine("        {");
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                
                // Register possible change for complex untracked classes
                if (prop.IsComplexUntrackedClass)
                {
                    sb.AppendLine("                RegisterPossibleChange(ref field);");
                }
                
                sb.AppendLine("                return field;");
                sb.AppendLine("            }");
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                sb.AppendLine("                SetField(ref field, value);");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }

            // Generate TrackChanges method
            sb.AppendLine();
            sb.AppendLine("        public override void TrackChanges(global::MongoObject.Core.Data.TrackingObservableObject observable, bool isTracking, string parentName)");
            sb.AppendLine("        {");
            sb.AppendLine("            ParentName = parentName;");
            sb.AppendLine("            PropertyChanged -= observable.Test_PropertyChanged;");
            sb.AppendLine("            PropertyChanged += observable.Test_PropertyChanged;");
            sb.AppendLine();
            sb.AppendLine("            if (isTracking)");
            sb.AppendLine("            {");
            sb.AppendLine("                Tracking = true;");

            foreach (var prop in model.Properties)
            {
                if (prop.IsBsonIgnore)
                {
                    // Skip properties marked with [BsonIgnore]
                    continue;
                }
                if (prop.IsTrackable || prop.IsMongoObject)
                {
                    // Trackable property - call TrackChanges on it
                    sb.AppendLine($"                if ({prop.Name} != null)");
                    sb.AppendLine($"                    {prop.Name}.TrackChanges(this, this.Tracking, \"{prop.Name}\");");
                }
                else if (prop.IsComplexUntrackedClass)
                {
                    // Complex untracked class - call OnPropertyChanged
                    sb.AppendLine($"                if ({prop.Name} != null)");
                    sb.AppendLine($"                    OnPropertyChanged({prop.Name}, $\"{{ParentName}}.{prop.Name}\");");
                }
            }

            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.g.cs", sb.ToString());
        }
    }
}