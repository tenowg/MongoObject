using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal record ValidationResult(bool HasInvalidOption, Location? Location, List<string> Errors, DiagnosticDescriptor Descriptor);
}
