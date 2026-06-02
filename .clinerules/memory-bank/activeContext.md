# Active Context

## Current Work Focus

The MongoObject project is in active development. Core functionality is implemented with documentation prepared for initial GitHub push. Source generator has been refactored for improved performance and structure.

## Recent Changes

### Source Generator Performance Refactoring (June 2026)
- **Equatable Model Types** - Replaced Roslyn symbols (`INamedTypeSymbol`, `IPropertySymbol`) with strings and pre-computed booleans in models. This fixes incremental caching - the generator now properly caches results and only re-runs when relevant code changes.
- **Removed Instance State** - Removed `_trackingSymbol` instance field from `CommonGenerator` to prevent stale state bugs across compilations.
- **StringBuilder Usage** - Converted all modules from string concatenation (`source += ...`) to `StringBuilder`, significantly reducing allocations.
- **CancellationToken Support** - Added `CancellationToken` checks throughout the pipeline for better IDE responsiveness.
- **Pre-computed Type Checks** - Symbol-based checks (`IsMongoObject`, `IsTrackable`, `IsComplexUntrackedClass`) are now computed once during model building instead of repeatedly in each module.
- **Single-Pass Property Processing** - Consolidated `ProcessProperties` and `ValidatePartialProperties` into a single `ProcessAllProperties` method.

### Module Separation (June 2026)
- **ObjectDiscoveryModule** - Now only generates the DI registration extension (`RegisterDocuments{Namespace}()`)
- **ExtensionModule** - New module generating per-class extension methods (`MetadataSearch`, `DocumentSearch`, `Add`) in the same namespace as the document class

### Bug Fixes (June 2026)
- **Nullable Type Handling** - Fixed `MetadataModule` to properly handle nullable types by stripping the `?` from `FullName` before adding our own.
- **Query Type Name Generation** - Fixed `DocumentSearchModule` to use `TypeName` (the property's type name) instead of the property name for generating query types (e.g., `BObjectQuery` instead of `NothingQuery`).
- **Nested MongoObject Query Type Mismatch (CS1503)** - Fixed by using `BsonDocument` as an intermediate type with `RenderArgs<T>` to convert nested filter types to parent filter types.

### Documentation Setup (June 2026)
- Root README.md created with comprehensive project overview
- /Docs folder configured with DocFX for GitHub Pages
- GitHub Actions workflow created for automated doc deployment
- Seven documentation articles written (getting started, defining documents, change tracking, metadata, searching, projections, dependency injection)
- LICENSE.txt files updated with correct year and author

### Core Features
- Source generator modules implemented for code generation
- Change tracking via `TrackingObservableObject` base class
- Document monitoring with `IDocumentMonitor<T>` interface
- Caching layer with `InternalCacheService`
- Distributed locking with `DistributedLockManager`
- MongoDB change stream watching via `MongoDocumentWatcher`

## Active Decisions and Considerations

### Documentation Architecture
- Using DocFX with modern template for documentation
- Documentation hosted at root `/Docs` folder (moved from MongoObject.Core/Docs)
- GitHub Pages deployment via GitHub Actions workflow
- API reference section prepared for when XML documentation comments are complete
- Manual articles focus on concepts and usage patterns

### Source Generator Architecture
- Using incremental source generators with proper equatable models for caching
- Modular design with separate modules for different concerns:
  - `ValidatorModule` - Validates document classes
  - `MetadataModule` - Generates metadata query/record types
  - `MongoObjectModule` - Generates main document partial class
  - `DocumentSearchModule` - Generates search classes with BsonDocument intermediate for nested queries
  - `ObjectDiscoveryModule` - Generates DI registration extension only
  - `ExtensionModule` - Generates per-class extension methods (MetadataSearch, DocumentSearch, Add)
  - `ProjectionModule` - Currently commented out, in development
- Models are equatable (no Roslyn symbols) for proper incremental caching
- All modules use StringBuilder for code generation
- CancellationToken checks for IDE responsiveness

### Cross-Assembly Support
- The `IsMongoObject` flag is computed during model building using `compilation.GetTypeByMetadataName()` and `prop.Type.GetAttributes()`
- This works correctly for types from referenced assemblies (e.g., BObject from AssemblyB used in AObject from AssemblyA)

### Change Tracking Approach
- Uses `INotifyPropertyChanged` pattern
- Tracks changes at property level for efficient updates
- Supports nested object tracking via parent-child relationships
- Generates `$set` and `$unset` operations based on changes

### Document Structure
- Documents wrapped in `MongoDocument<T>` with separate `Document` and `Metadata` fields
- Metadata stored as `BsonDocument` for flexibility
- Automatic version increment and timestamp updates on save

## Next Steps
1. Complete projection module implementation (has errors that need fixing)
2. Implement delete operations (currently empty)
3. Add polling-based watch mode (currently only stream-based)
4. Complete XML documentation comments for API reference
5. Add more comprehensive testing
6. Publish initial release to NuGet

## Important Patterns and Preferences

### Code Style
- Uses C# 14 features (partial properties, extensions)
- Nullable reference types enabled
- Implicit usings enabled
- Primary constructors for services

### Architecture
- Dependency injection throughout
- Interface-based design for testability
- Generic types for type safety
- Async/await for all I/O operations

### Documentation
- DocFX for static site generation
- GitHub Actions for CI/CD of documentation
- Articles focus on practical usage examples
- API reference auto-generated from XML comments (pending completion)

## Learnings and Project Insights
- **Incremental source generators require equatable models** - Roslyn symbols don't implement value equality, causing cache misses on every keystroke. Use strings and pre-computed booleans instead.
- **Source generators should be stateless** - Instance fields can cause stale data bugs across compilations.
- **StringBuilder is essential for code generation** - String concatenation creates hundreds of intermediate allocations.
- **CancellationToken checks improve IDE responsiveness** - Allows the generator to stop early when the user types again.
- **BsonDocument intermediate solves nested filter type mismatches** - Use `RenderArgs<T>` and `BsonDocumentFilterDefinition<T>` to convert between filter types.
- Partial properties in C# 14 enable clean source generation
- MongoDB change streams require `directConnection=true` for local development
- Memory cache needs both absolute and sliding expiration for optimal performance
- Distributed locking is essential for concurrent document access
- DocFX modern template provides clean, responsive documentation UI
- GitHub Pages with Actions provides free, automated documentation hosting