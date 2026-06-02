using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Models
{
    internal record ValidationResult(bool HasInvalidOption, Location? Location, List<string> Errors, DiagnosticDescriptor Descriptor);
}
