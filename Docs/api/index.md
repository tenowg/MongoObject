# API Reference

The API reference documentation is auto-generated from the MongoObject.Core source code using DocFX.

---

## Status

> **🚧 API Documentation In Progress**
>
> We are actively adding XML documentation comments to the codebase. The complete API reference will be available once the documentation comments are finalized.

---

## Core Namespaces

### MongoObject.Core.Attributes

Attributes for defining and configuring MongoDB documents:

| Attribute | Description |
|-----------|-------------|
| `[MongoObject]` | Marks a class as a MongoDB document with collection and database configuration |
| `[ProjectValue]` | Configures projection behavior for properties |
| `[PropertyNameChange]` | Maps property name changes for backward compatibility |

### MongoObject.Core.Data

Core data types and base classes:

| Type | Description |
|------|-------------|
| `MongoDocument<T>` | Wrapper for documents with Id, Document, and Metadata |
| `TrackingObservableObject` | Base class providing change tracking functionality |
| `MongoObjectOptions` | Configuration options for MongoObject |
| `SaveChangesResult` | Result of a SaveChanges operation |

### MongoObject.Core.Interfaces

Core interfaces for interacting with MongoObject:

| Interface | Description |
|-----------|-------------|
| `IDocumentMonitor<T>` | Main API for CRUD operations on documents |
| `IDocumentFile` | Interface implemented by all document classes |
| `IMongoLockScope` | Interface for distributed lock scopes |
| `IMetadataBase` | Base interface for metadata types |

### MongoObject.Core.Extensions

Extension methods for dependency injection:

| Extension | Description |
|-----------|-------------|
| `AddMongoObject()` | Registers MongoObject services in the DI container |
| `RegisterDocumentsFromAssembly()` | Registers all document types from an assembly |
| `AddWatchStream()` | Enables MongoDB change stream monitoring |

---

## Coming Soon

The following sections will be expanded with detailed API documentation:

- Complete type reference with all members
- Method signatures and parameters
- Code examples for each type
- Inheritance hierarchies
- Thread safety information

---

## Quick Links

- [Getting Started](../articles/getting-started.md)
- [Defining Documents](../articles/defining-documents.md)
- [Change Tracking](../articles/change-tracking.md)
- [GitHub Repository](https://github.com/tenowg/MongoObjects)