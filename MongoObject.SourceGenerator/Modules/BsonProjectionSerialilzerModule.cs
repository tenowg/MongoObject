using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class BsonProjectionSerialilzerModule : CodeModule
    {
        public override void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            foreach (var projection in provider.model.Projections)
            {
                var sb = new StringBuilder();
                sb.AppendLine("using MongoDB.Bson.IO;");
                sb.AppendLine();
                sb.AppendLine($"public class {model.Name}{projection.Name}Serializer : global::MongoDB.Bson.Serialization.Serializers.SerializerBase<global::{model.Namespace}.{model.Name}{projection.Name}>");
                sb.AppendLine("{");
                sb.AppendLine($"    public override void Serialize(global::MongoDB.Bson.Serialization.BsonSerializationContext context, global::MongoDB.Bson.Serialization.BsonSerializationArgs args, global::{model.Namespace}.{model.Name}{projection.Name} value)");
                sb.AppendLine("    {");
                sb.AppendLine("        var bsonWriter = context.Writer;");
                sb.AppendLine("        bsonWriter.WriteStartDocument();");
                sb.AppendLine("        // Implementation for serialization");
                sb.AppendLine("        bsonWriter.WriteEndDocument();");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine($"    public override {model.Namespace}.{model.Name}{projection.Name} Deserialize(global::MongoDB.Bson.Serialization.BsonDeserializationContext context, global::MongoDB.Bson.Serialization.BsonDeserializationArgs args)");
                sb.AppendLine("    {");
                sb.AppendLine("        var bsonReader = context.Reader;");
                sb.AppendLine("        bsonReader.ReadStartDocument();");
                sb.AppendLine();
                sb.AppendLine($"        var result = new global::{model.Namespace}.{model.Name}{projection.Name}();");
                sb.AppendLine();
                // Loop through the fields returned by MongoDB
                sb.AppendLine($"        while (bsonReader.ReadBsonType() != global::MongoDB.Bson.BsonType.EndOfDocument)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var name = bsonReader.ReadName();");
                sb.AppendLine("            if (name == \"Document\") // Handle nested properties safely");
                sb.AppendLine("            {");
                sb.AppendLine("                bsonReader.ReadStartDocument();");
                sb.AppendLine("                while (bsonReader.ReadBsonType() != global::MongoDB.Bson.BsonType.EndOfDocument)");
                sb.AppendLine("                {");
                sb.AppendLine("                    var subName = bsonReader.ReadName();");
                foreach (var prop in projection.Properties)
                {
                    if (prop.IsBsonIgnore) continue; // Skip ignored properties
                    if (prop.EnumName == "Slice")
                    {
                        sb.AppendLine($"                    if (subName == \"{prop.QueryName}\")");
                        sb.AppendLine("                    {");
                        sb.AppendLine("                        // Deserialize the list directly into your flat property");
                        sb.AppendLine("                        result.ListTest = global::MongoDB.Bson.Serialization.BsonSerializer");
                        sb.AppendLine($"                           .Deserialize<{prop.FullName}>(bsonReader);");
                        sb.AppendLine("                        continue;");
                        sb.AppendLine("                    }");
                    } 
                    else if (!prop.IsBsonIgnore && prop.EnumName != "Exclude")
                    {
                        sb.AppendLine($"                    if (subName == \"{prop.QueryName}\")");
                        sb.AppendLine("                    {");
                        sb.AppendLine("                        // Deserialize the value directly into the property");
                        sb.AppendLine($"                        result.{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<{prop.FullName}>(bsonReader);");
                        sb.AppendLine("                        continue;");
                        sb.AppendLine("                    }");
                    }
                }
                
                sb.AppendLine("                    bsonReader.SkipValue();");
                sb.AppendLine("                }");
                sb.AppendLine("                bsonReader.ReadEndDocument();");
                sb.AppendLine("            }");


                //sb.AppendLine("                    var subName = bsonReader.ReadName();");
                foreach (var prop in projection.Properties)
                {
                    if (prop.IsBsonIgnore) continue; // Skip ignored properties
                    if (prop.EnumName == "Slice")
                    {
                        sb.AppendLine($"            if (name == \"{prop.QueryName}\")");
                        sb.AppendLine("            {");
                        sb.AppendLine("                // Deserialize the list directly into your flat property");
                        sb.AppendLine("                result.ListTest = global::MongoDB.Bson.Serialization.BsonSerializer");
                        sb.AppendLine($"                   .Deserialize<{prop.FullName}>(bsonReader);");
                        sb.AppendLine("                continue;");
                        sb.AppendLine("            }");
                    }
                    else if (!prop.IsBsonIgnore && prop.EnumName != "Exclude")
                    {
                        sb.AppendLine($"            if (name == \"{prop.QueryName}\")");
                        sb.AppendLine("            {");
                        sb.AppendLine("                // Deserialize the value directly into the property");
                        sb.AppendLine($"                result.{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<{prop.FullName}>(bsonReader);");
                        sb.AppendLine("                continue;");
                        sb.AppendLine("            }");
                    }

                    if (!prop.IsBsonIgnore && (prop.EnumName == "Vector" || prop.EnumName == "AutoVector"))
                    {
                        sb.AppendLine($"            if (name == \"Score\")");
                        sb.AppendLine("            {");
                        sb.AppendLine("                // Deserialize the value directly into the property");
                        sb.AppendLine($"                result.Score = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<float>(bsonReader);");
                        sb.AppendLine("                continue;");
                        sb.AppendLine("            }");
                    }
                }

                sb.AppendLine("            if (bsonReader.State == BsonReaderState.Type)");
                sb.AppendLine("            {");
                sb.AppendLine("                bsonReader.ReadBsonType();");
                sb.AppendLine("            }");
                sb.AppendLine("            if (bsonReader.State == BsonReaderState.Type)");
                sb.AppendLine("            {");
                sb.AppendLine("                bsonReader.ReadName();");
                sb.AppendLine("            }");
                sb.AppendLine("            bsonReader.SkipValue(); // Skips fields like _id if they leak through");
                
                sb.AppendLine("        }");
                sb.AppendLine("        bsonReader.ReadEndDocument();");
                sb.AppendLine("        return result;");
                sb.AppendLine("    }");
                sb.AppendLine("}");


                context.AddSource($"{provider.model.Name}_{projection.Name}_BsonProjectionSerializer.g.cs", sb.ToString());
            }
        }
    }
}
