# MongoObject.SourceGenerator

**The Roslyn incremental source generator for MongoObject — automatic code generation for MongoDB document classes.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-239120)](https://learn.microsoft.com/en-us/dotnet/csharp/)

---

## Overview

MongoObject.SourceGenerator is a Roslyn incremental source generator that automatically generates boilerplate code for classes decorated with the `[MongoObject]` attribute. It eliminates the need for manual implementation of change tracking, metadata types, search classes, and dependency injection registration.

The generator is bundled with `MongoObject.Core` as an analyzer reference — no separate installation is required.

---

## How It Works

When you decorate a partial class with `[MongoObject]`, the source generator:

1. **Validates** the class structure at compile time (reports diagnostics for invalid configurations)
2. **Generates** partial property implementations with automatic change tracking
3. **Creates** metadata query and record types for metadata management
4. **Generates** type-safe search classes for document and metadata queries
5. **Produces** extension methods for `IDocumentMonitor<T>` (Add, DocumentSearch, MetadataSearch)
6. **Generates** a DI registration extension method for bulk document registration

### Example Input

```csharp
using MongoObject.Core.Attributes;

public partial record UserMeta
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
}

[MongoObject(
    CollectionName = "Users",
    DatabaseName = "MyApp",
    MetadataType = typeof(UserMeta)
)]
public partial class User
{
    public partial string Name { get; set; }
    public partial int Age { get; set; }
    public partial Address Address { get; set; }
}
```

### Generated Output

The generator produces the following files for each `[MongoObject]` class:

| Generated File | Description |
|---------------|-------------|
| `{Name}.g.cs` | Partial class with property implementations, change tracking, and interface implementations |
| `{Name}.MetaQuery.g.cs` | Metadata query type (`{MetaName}Query`) with `ToMongoFilter<T>()` |
| `{Name}.Meta.g.cs` | Metadata record type (`{MetaName}Record`) with `IMetadataBase` |
| `{Name}.Query.g.cs` | Document search type (`{Name}Query`) with `ToMongoFilter()` |
| `{Name}.Extensions.g.cs` | Extension methods: `Add()`, `DocumentSearch()`, `MetadataSearch()` |
| `{Namespace}_ObjectDiscovery.g.cs` | DI registration extension: `RegisterDocuments{Namespace}()` |

---

## Architecture

### Incremental Generator with Equatable Models

The generator uses Roslyn's incremental source generator API with **equatable models** for proper caching:

```
SyntaxProvider → BuildCommonModel (equatable) → Cache Check → Execute Modules
```

**Why equatable models matter:**
- Roslyn symbols (`INamedTypeSymbol`, `IPropertySymbol`) don't implement value equality
- Without equatable models, the incremental cache always misses, causing the generator to re-run on every keystroke
- Models contain only strings and pre-computed booleans — no Roslyn symbols
- This ensures the cache works correctly and the generator only re-runs when relevant code changes

### Performance Optimizations

| Optimization | Description |
|-------------|-------------|
| **Equatable Models** | `CommonModel` and `PropertyModel` use strings and primitives instead of Roslyn symbols |
| **StringBuilder** | All modules use `StringBuilder` instead of string concatenation to reduce allocations |
| **CancellationToken** | Checks throughout the pipeline for IDE responsiveness |
| **Pre-computed Type Checks** | `IsMongoObject`, `IsTrackable`, `IsComplexUntrackedClass` computed once during model building |
| **Single-Pass Processing** | Properties are validated and modeled in a single pass (`ProcessAllProperties`) |
| **Stateless Generator** | No instance fields — prevents stale data across compilations |

---

## Modules

The generator uses a modular architecture. Each module implements either `ICodeModule` (per-document processing) or `ICodeModuleMultiple` (batch processing across all documents).

### Module Pipeline

```
┌─────────────────────┐
│   ValidatorModule   │  Reports compile-time diagnostics
├─────────────────────┤
│   MetadataModule    │  Generates {Meta}Query and {Meta}Record types
├─────────────────────┤
│  MongoObjectModule  │  Generates partial class with property implementations
├─────────────────────┤
│ DocumentSearchModule│  Generates {Name}Query search class
├─────────────────────┤
│   ExtensionModule   │  Generates per-class extension methods
├─────────────────────┤
│ ObjectDiscoveryModule│ Generates DI registration extension (batch)
└─────────────────────┘
```

### ValidatorModule

**Type:** `ICodeModule`

Validates document class structure and reports compile-time diagnostics:

- Non-partial public properties with getters/setters produce an error (must use `partial`)
- Metadata type properties named `Version`, `LastModifiedAt`, or `CreatedAt` produce a warning (reserved names)
- Non-nullable metadata properties produce an error (metadata properties must be nullable)

### MetadataModule

**Type:** `ICodeModule`

Generates two types for metadata management:

**`{MetaName}Query`** — A query type for searching by metadata fields:
```csharp
public partial record UserMetaQuery : MetadataSearch, IMetadataSearchBase
{
    public QueryVal<DateTime>? CreatedAt { get; set; }
    public QueryVal<DateTime>? LastModifiedAt { get; set; }
    public QueryVal<int>? Version { get; set; }
    public QueryVal<string>? CreatedBy { get; set; }
    public QueryVal<string>? Department { get; set; }

    public FilterDefinition<MongoDocument<T>> ToMongoFilter<T>() { ... }
}
```

**`{MetaName}Record`** — A record type for setting metadata on new documents:
```csharp
public partial record UserMetaRecord : IMetadataBase
{
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public int? Version { get; set; }
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
}
```

### MongoObjectModule

**Type:** `ICodeModule` (extends `CodeModule`)

Generates the main partial class implementation:

- Inherits from `TrackingObservableObject`
- Implements `IDocumentFile`, `IDocumentFileInternal`, `IDocumentFile<TMetaQuery, TMetaRecord>`
- Generates partial property implementations with `SetField()` for change tracking
- Generates `TrackChanges()` override for nested object tracking
- Handles three property categories:
  - **Trackable** (`IsTrackable` / `IsMongoObject`): Nested objects that propagate change tracking
  - **Complex Untracked** (`IsComplexUntrackedClass`): Regular classes tracked as whole objects via `RegisterPossibleChange()`
  - **Simple types**: Tracked directly via `SetField()`

```csharp
// Generated
public partial class User : TrackingObservableObject, IDocumentFile, IDocumentFileInternal, IDocumentFile<UserMetaQuery, UserMetaRecord>
{
    public Type GetSearchMetaType() => typeof(UserMetaQuery);
    public Type GetRecordMetaType() => typeof(UserMetaRecord);
    public string GetDatabaseName() => "MyApp";
    public string GetCollectioName() => "Users";

    public partial string Name
    {
        get => field;
        set => SetField(ref field, value);
    }

    public partial int Age
    {
        get => field;
        set => SetField(ref field, value);
    }

    public partial Address Address
    {
        get { RegisterPossibleChange(ref field); return field; }
        set => SetField(ref field, value);
    }

    public override void TrackChanges(TrackingObservableObject observable, bool isTracking, string parentName) { ... }
}
```

### DocumentSearchModule

**Type:** `ICodeModule` (extends `CodeModule`)

Generates a type-safe search class for document field queries:

```csharp
public record UserQuery : MetadataSearch, IClassSearch, IClassSearch<User>
{
    public QueryVal<string>? Name { get; set; }
    public QueryVal<int>? Age { get; set; }
    public AddressQuery Address { get; set; }  // Nested query for MongoObject properties

    public FilterDefinition<MongoDocument<User>> ToMongoFilter(string prefix = "") { ... }
}
```

**Nested MongoObject handling:** For properties that are themselves `[MongoObject]` types, the generated `ToMongoFilter()` uses `BsonDocument` as an intermediate type with `RenderArgs<T>` to convert between filter types. This is necessary because `FilterDefinition<T>` cannot be directly converted between different `T` types.

### ExtensionModule

**Type:** `ICodeModule`

Generates per-class extension methods in the same namespace as the document class:

```csharp
public static class UserExtensions
{
    // Search by metadata fields
    public static async Task<IEnumerable<User>> MetadataSearch(
        this IDocumentMonitor<User> monitor,
        Action<UserMetaQuery> configure) { ... }

    // Search by document fields
    public static async Task<IEnumerable<User>> DocumentSearch(
        this IDocumentMonitor<User> monitor,
        Action<UserQuery> configure) { ... }

    // Add with metadata configuration
    public static async Task<string> Add(
        this IDocumentMonitor<User> monitor,
        User document,
        Action<UserMetaRecord> configure) { ... }
}
```

These extensions are generated in the document's namespace so they're available without additional `using` statements.

### ObjectDiscoveryModule

**Type:** `ICodeModuleMultiple` (batch processing)

Generates a single DI registration extension method that registers all `[MongoObject]` classes found in the compilation:

```csharp
// Generated in {RootNamespace}.Extensions namespace
internal static class ObjectDiscovery
{
    extension(MongoObjectBuilder builder)
    {
        public MongoObjectBuilder RegisterDocumentsMyNamespace()
        {
            builder.RegisterDocument<User, UserMetaQuery, UserMetaRecord>();
            builder.RegisterDocument<Order, OrderMetaQuery, OrderMetaRecord>();
            // ... all discovered document types
            return builder;
        }
    }
}
```

### ProjectionModule

**Type:** `ICodeModule` (extends `CodeModule`)

**Status:** Currently commented out / in development.

Will generate projection types based on `[ProjectValue]` attributes for selective field retrieval.

---

## Models

### CommonModel

The equatable model passed through the pipeline. Contains no Roslyn symbols:

```csharp
internal sealed record CommonModel
{
    public string Namespace { get; init; }
    public string Name { get; init; }
    public string DatabaseName { get; init; }
    public string CollectionName { get; init; }
    public IReadOnlyList<PropertyModel> Properties { get; init; }
    public MetadataModel Metadata { get; init; }
    public IReadOnlyList<ProjectionModel> Projections { get; init; }
    public IReadOnlyList<ValidationResult> Errors { get; init; }
}
```

### PropertyModel

Equatable property descriptor with pre-computed type checks:

```csharp
internal sealed record PropertyModel
{
    public string Name { get; init; }
    public string FullName { get; init; }
    public string TypeName { get; init; }
    public string UnderlyingTypeName { get; init; }
    public bool IsNullable { get; init; }
    public bool IsNumeric { get; init; }
    public bool IsMongoObject { get; init; }          // Pre-computed
    public bool IsTrackable { get; init; }             // Pre-computed
    public bool IsComplexUntrackedClass { get; init; } // Pre-computed
}
```

### MetadataModel

Describes the metadata type and its properties:

```csharp
internal sealed record MetadataModel
{
    public string Name { get; init; }
    public IReadOnlyList<PropertyModel> Properties { get; init; }
}
```

---

## Cross-Assembly Support

The generator correctly handles types from referenced assemblies. For example, if `BObject` (from AssemblyB) is used as a property type in `AObject` (from AssemblyA), the `IsMongoObject` flag is computed correctly using `compilation.GetTypeByMetadataName()` and attribute lookup, which works across assembly boundaries.

---

## Debugging

To debug the source generator:

1. Uncomment `Debugger.Launch()` in `CommonGenerator.cs`
2. Build the consuming project
3. Attach the debugger when prompted

---

## Requirements

- **.NET Standard 2.0** (target framework for source generator compatibility)
- **Microsoft.CodeAnalysis.CSharp 5.3.0** (Roslyn APIs)
- **C# 14** language features in consuming projects (for partial properties)

---

## Project Structure

```
MongoObject.SourceGenerator/
├── Generators/
│   ├── CommonGenerator.cs          # Main incremental generator entry point
│   └── CommonGenerator.1.cs        # Additional generator helpers
├── Interfaces/
│   └── ICodeModule.cs              # Module interfaces (ICodeModule, ICodeModuleMultiple, CodeModule)
├── Models/
│   ├── CommonModel.cs              # Equatable document model
│   ├── PropertyModel.cs            # Equatable property model
│   ├── MetadataModel.cs            # Metadata type model
│   ├── ProjectionModel.cs          # Projection model
│   ├── ValidationResult.cs         # Validation error descriptor
│   ├── DeclaredDiagnosticDescriptor.cs # Diagnostic definitions
│   └── IsExternalInit.cs           # Polyfill for record support on netstandard2.0
├── Modules/
│   ├── ValidatorModule.cs          # Compile-time validation
│   ├── MetadataModule.cs           # Metadata query/record generation
│   ├── MongoObjectModule.cs        # Partial class generation
│   ├── DocumentSearchModule.cs     # Search class generation
│   ├── ExtensionModule.cs          # Extension method generation
│   ├── ObjectDiscoveryModule.cs    # DI registration generation
│   └── ProjectionModule.cs         # Projection generation (WIP)
└── Tenowg.MongoObject.props        # MSBuild props for package consumers
```

---

## License

This project is licensed under the MIT License — see the [LICENSE.txt](LICENSE.txt) file for details.

---

## Related

- **[MongoObject.Core](../MongoObject.Core/README.md)** — The core runtime library
- **[Full Documentation](../Docs/index.md)** — Comprehensive guides and API reference
- **[Root README](../README.md)** — Project overview and quick start