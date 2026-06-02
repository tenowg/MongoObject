using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Models
{
    internal class DeclaredDiagnosticDescriptor
    {
        public static DiagnosticDescriptor InvalidPropertyTypeDescriptor { get; set; } = new DiagnosticDescriptor(
            id: "SG0001",
            title: "Invalid property type used",
            messageFormat: "The property '({1}){0}' is invalid. Metadata record constructor type must be of type QueryVal<{1}>.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error, // This fails the build
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidPartialPropertyDescriptor { get; set; } = new DiagnosticDescriptor(
            id: "CS0260",
            title: "Invalid partial property used",
            messageFormat: "The property '({1}){0}' is invalid. Property must be partial.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning, // This fails the build
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidPropertyNameReservedDescriptor { get; set; } = new DiagnosticDescriptor(
            id: "SG0003",
            title: "Invalid property name used",
            messageFormat: "The property '({1}){0}' is invalid. Property name is reserved.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error, // This fails the build
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidPropertyNonNullableDescriptor { get; set; } = new DiagnosticDescriptor(
            id: "SG0004",
            title: "Invalid property, property must be nullable.",
            messageFormat: "The property '({1}){0}' is invalid. Property is not Nullable.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error, // This fails the build
            isEnabledByDefault: true);
    }
}
