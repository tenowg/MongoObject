# Product Context: MongoObject

## Why This Project Exists

MongoObject wasn't built because MongoDB.Driver is bad — it was built because even experienced MongoDB developers have to stop and think every time they write a `Builders<T>.Filter` query. The API is powerful but not discoverable. MongoObject makes the right thing the obvious thing.

## Problems Solved

### 1. String-Based Queries
Developers must write field names as strings (`"Name"`, `"Age"`), which are prone to typos and break at runtime when property names change.

**Before:** `Builders<User>.Filter.Eq(u => u.Name, "John")` — compiles but uses string internally  
**After:** Generated query types with full compile-time validation of field names

### 2. Manual Update Definitions
Constructing `$set`, `$unset`, and other update operators requires manual boilerplate code.

**Before:** Manually building `UpdateDefinition<T>` for partial updates  
**After:** Automatic change tracking sends only modified fields to MongoDB

### 3. Reflection-Based Mapping
Traditional ODMs rely on reflection at runtime, which:
- Adds performance overhead
- Breaks with AoT/NativeAOT compilation
- Loses IDE support (go-to-definition, refactoring)

**After:** All mapping is resolved at compile time via Roslyn source generators

### 4. Schema Drift
As models evolve, MongoDB documents can become inconsistent with the code model.

**After:** CLI tool (`mo migrate build`) generates validation schemas and tracks renamed properties

## How It Works

MongoObject bridges the gap between MongoDB's document model and modern .NET development by:

1. **Decorating POCOs** — Add `[MongoObject]` attribute to your class
2. **Compile-time generation** — Roslyn source generator produces:
   - Partial property implementations with change tracking
   - Metadata query types for versioning/timestamps
   - Type-safe search classes for queries
3. **Runtime integration** — `MongoDocumentManager<T>` provides CRUD operations, caching, and search

## User Experience Goals

- Write plain C# classes and get a fully functional MongoDB document system
- Get compile-time errors instead of runtime exceptions when field names change
- See minimal update payloads sent to the database (only changed fields)
- Have full IDE support for all document operations
- Deploy with confidence using AoT-compatible code

## Target Users

- .NET developers working with MongoDB who want type safety and less boilerplate
- Teams migrating from EF Core or similar ORMs who prefer MongoDB's document model
- Performance-conscious applications requiring AoT compatibility
- Projects needing vector search capabilities alongside standard CRUD operations
