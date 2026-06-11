using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Encryption.Interfaces;
using MongoObject.SourceGenerator.Encryption.Models;
using System.Collections.Generic;
using System.Linq;

namespace MongoObject.SourceGenerator.Encryption.Modules
{
    internal class ValidatorModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, ((CommonModel model, EncryptedPropertyModel encrypted) models, Compilation comp) provider)
        {
            ((CommonModel model, EncryptedPropertyModel encrypted) allModels, Compilation comp) = provider;
            var model = allModels.model;

            foreach (var error in model.Errors)
            {
                var diagnostic = Diagnostic.Create(
                    error.Descriptor,
                    location: error.Location,
                    messageArgs: error.Errors.ToArray()
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}

