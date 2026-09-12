# System Patterns: MongoObject

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    Application Layer                  │
│              (POCO Models + [MongoObject])           │
├─────────────────────────────────────────────────────┤
│                 Source Generator Layer               │
│         (Roslyn generates at compile time)            │
├─────────────────────────────────────────────────────┤
│                   Core Library Layer                 │
│   MongoDocumentManager  │  Services  │  Extensions   │
├─────────────────────────────────────────────────────┤
│              Extension Packages                       │
│  DistributedLock  │  Encryption  │  CLI Tool         │
├─────────────────────────────────────────────────────┤
│                  MongoDB.Driver                        │
└─────────────────────────────────────────────────────┘
```

## Key Components

### 1. Source Generator (`MongoObject.SourceGenerator`)
- **Target:** netstandard2.0, C# 14
- **Dependencies:** Microsoft.CodeAnalysis.CSharp 5.3.0, Microsoft.Bcl.Memory 10.0.8
- **Function:** Analyzes classes with `[MongoObject]` attribute and generates:
  - Partial property implementations with change tracking
  - Metadata query types (versioning, timestamps)
  - Type-safe search/query classes
  - Projection definitions

### 2. Core Library (`MongoObject.Core`)
- **Package:** `Tenowg.MongoObjects`
- **Dependencies:** MongoDB.Driver 3.8.0, SharpCompress, Snappier, ProtectedData
- **Key Classes:**
  - `MongoDocument<T>` — Document wrapper with Id, Document, and Metadata
  - `MongoDocumentManager<T>` — CRUD operations, caching, search
  - `TrackingObservableObject` — Change tracking implementation

### 3. Attributes (`MongoObject.Core/Attributes`)
| Attribute | Purpose |
|-----------|---------|
| `[MongoObject]` | Marks class for source generation |
| `[MongoIndex]` | Defines MongoDB index configuration |
| `[FieldIndex]` | Field-level index specification |
| `[MigrationSchema]` | Schema validation and migration policy |
| `[ProjectValue]` | Controls field inclusion in projections |
| `[MongoEmbedded]` | Marks embedded/nested documents |
| `[PropertyNameChange]` | Tracks renamed properties for migrations |

### 4. Extension Packages
| Package | Purpose |
|---------|---------|
| `Tenowg.MongoObjects.CliTool` | CLI for migration, schema validation, property rename tracking |
| `Tenowg.MongoObjects.MongoDistributedLock` | MongoDB-based document-level distributed locking |
| `Tenowg.MongoObjects.PropertyEncryption` | Property-level encryption support |
| `Tenowg.MongoObjects.RedisDistributedLock` | Redis-based distributed locking |
| `Tenowg.MongoObjects.SourceGenerator.Encryption` | Encryption source generator |
| `Tenowg.MongoObjects.Templates` | Project templates |

## Key Design Patterns

### Source Generation Pattern
```csharp
[MongoObject]
public partial class User
{
    public string? Name { get; set; }
    public int? Age { get; set; }
}
// Generated: partial property implementations with change tracking,
// UserSearch (type-safe query builder), metadata types
```

### Change Tracking Pattern
- Uses `INotifyPropertyChanged` interface
- BsonValue snapshots capture property values at key points
- Only modified fields generate update operations ($set/$unset)
- Minimal MongoDB payloads reduce network and storage overhead

### Document Wrapper Pattern
```csharp
public class MongoDocument<T>
{
    public string Id { get; set; }           // MongoDB _id
    public T? Document { get; set; }         // Business data
    public BsonDocument Metadata { get; set; } // Version, timestamps, ownership
}
```

### Search Builder Pattern
Generated search classes provide fluent API:
```csharp
var results = await monitor.Search<User>()
    .WithQuery(f => f.Name == "John")
    .WithFilter(f => f.Age > 18)
    .WithSorting(f => f.Name, SortDirection.Ascending)
    .WithSummaryProjection();
```

## Component Relationships

```
[MongoObject] attribute
        │
        ▼
┌──────────────┐     generates      ┌─────────────────┐
│  Source       │──────────────────►│  Partial class   │
│  Generator    │                   │  implementations │
└──────────────┘                   └─────────────────┘
        │                                    │
        ▼                                    ▼
┌──────────────┐                   ┌─────────────────┐
│  Search       │                   │  MongoDocument   │
│  Classes      │◄────────────────►│  Manager         │
└──────────────┘                   └─────────────────┘
                                              │
                                    ┌─────────▼─────────┐
                                    │  MongoDB.Driver    │
                                    └───────────────────┘
```

## Key Technical Decisions

1. **C# 14 Partial Properties** — Required for generating property implementations without base class inheritance or runtime proxies
2. **AoT Compatible** — No reflection; all mapping resolved at compile time
3. **MongoDB.Driver Direct** — Uses MongoDB.Driver directly rather than wrapping it, preserving native feature access
4. **BsonValue Snapshots** — Used for accurate change detection in property tracking
5. **MinVer Versioning** — Git tag-based semantic versioning
