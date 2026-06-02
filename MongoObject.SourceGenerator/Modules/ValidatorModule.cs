using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Collections.Generic;
using System.Linq;

namespace MongoObject.SourceGenerator.Modules
{
    internal class ValidatorModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            (CommonModel model, Compilation comp) = provider;

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

