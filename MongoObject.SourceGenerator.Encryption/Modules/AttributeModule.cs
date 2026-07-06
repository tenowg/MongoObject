using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Encryption.Helpers;
using MongoObject.SourceGenerator.Encryption.Interfaces;
using MongoObject.SourceGenerator.Encryption.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MongoObject.SourceGenerator.Encryption.Modules
{
    internal class AttributeModule : CodeModule
    {
        public override void Execute(SourceProductionContext context, ((CommonModel model, EncryptedClassModel encrypted) models, Compilation comp) provider)
        {
            //Debugger.Launch();
            var model = provider.models.model;
            if (model == null) return;
            var sb = new IndentedStringBuilder();

            sb.AppendLine("// Auto Generated File");
            sb.AppendLine($"namespace {model.Namespace}");
            using (sb.Block())
            {
                sb.AppendLine("public class KmsDefinitions");
                using (sb.Block())
                {
                    foreach (var prop in model.Properties)
                    {
                        if (prop.Local is not null)
                        {
                            sb.AppendLine($"public const string {prop.Name}LocalDefinition = \"{prop.Local.Key}\";");
                        }
                        if (prop.Aws is not null)
                        {
                            sb.AppendLine($"public const string {prop.Name}AwsDefinition = \"{prop.Aws.Key}\";");
                        }
                        if (prop.Azure is not null)
                        {
                            sb.AppendLine($"public const string {prop.Name}AzureDefinition = \"{prop.Azure.Key}\";");
                        }
                    }
                }
            }

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.Attributes.g.cs", sb.ToString());
        }
    }
}
