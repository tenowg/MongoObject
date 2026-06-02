# Progress

## What Works

### Core Functionality ✅
- **Document Definition**: `[MongoObject]` attribute with collection/database configuration
- **Partial Properties**: C# 14 partial properties for tracked fields
- **Change Tracking**: Automatic property change detection via `TrackingObservableObject`
- **Nested Object Tracking**: Parent-child relationship tracking
- **Document CRUD**:
  - Add documents with metadata
  - Get documents by ID (with caching)
  - Update documents with efficient pipeline generation
  - Document search (class-based and metadata-based)
- **Caching**: Memory cache with absolute and sliding expiration
- **Distributed Locking**: Document-level locking support
- **Change Stream Watching**: Real-time MongoDB change monitoring

### Documentation ✅
- **Root README.md**: Comprehensive project overview with quick start guide
- **DocFX Setup**: `/Docs` folder configured for GitHub Pages deployment
- **GitHub Actions**: Automated documentation build and deploy workflow
- **Articles Written**:
  - Getting Started
  - Defining Documents
  - Change Tracking
  - Metadata
  - Searching
  - Projections
  - Dependency Injection
- **API Reference**: Placeholder ready for XML documentation extraction
- **LICENSE**: MIT License with correct attribution (2026 Craig Russell)

### Source Generator Modules ✅
| Module | Status | Description |
|--------|--------|-------------|
| ValidatorModule | ✅ Complete | Validates document class structure |
| MetadataModule | ✅ Complete | Generates metadata query/record types |
| MongoObjectModule | ✅ Complete | Generates document partial class |
| DocumentSearchModule | ✅ Complete | Generates search classes with BsonDocument intermediate for nested queries |
| ObjectDiscoveryModule | ✅ Complete | Generates DI registration extension only |
| ExtensionModule | ✅ Complete | Generates per-class extension methods (MetadataSearch, DocumentSearch, Add) |
| ProjectionModule | 🚧 WIP | Projection support (commented out) |

### Source Generator Performance (June 2026) ✅
- **Equatable Models**: Replaced Roslyn symbols with strings and pre-computed booleans for proper incremental caching
- **StringBuilder**: All modules use StringBuilder instead of string concatenation
- **CancellationToken**: Added checks throughout the pipeline for IDE responsiveness
- **Pre-computed Type Checks**: IsMongoObject, IsTrackable, IsComplexUntrackedClass computed during model building
- **Single-Pass Processing**: Consolidated ProcessProperties and ValidatePartialProperties
- **Stateless Generator**: Removed instance fields to prevent stale data bugs

### Infrastructure ✅
- Dependency injection setup
- MongoDB connection management
- Transaction support for updates
- Key management for document tracking
- BsonDocument serialization

## What's Left to Build

### High Priority
- [ ] **Delete Operations**: `DeleteDocument()` method is empty
- [ ] **Projection Module**: Complete projection generation and runtime API
- [ ] **Polling Watch Mode**: `AddWatchPolling()` not implemented
- [ ] **XML Documentation**: Complete XML comments for full API reference

### Medium Priority
- [ ] **Batch Operations**: Bulk add/update support
- [ ] **Query Optimization**: Index hints, projection pushdown
- [ ] **Error Handling**: More specific exception types
- [ ] **Validation**: Runtime validation of document state

### Low Priority
- [ ] **Testing**: Unit and integration tests
- [ ] **Performance**: Benchmarking and optimization
- [ ] **Migrations**: Schema versioning support
- [ ] **NuGet Publishing**: Package preparation and publishing

## Current Status

### Version
Using MinVer for semantic versioning based on git tags.

### Build Status
- Debug: Builds successfully
- Release: Generates NuGet package

### Documentation Status
- README.md: ✅ Complete
- DocFX Configuration: ✅ Complete
- GitHub Actions Workflow: ✅ Complete
- Manual Articles: ✅ 7 articles written
- API Reference: 🚧 Placeholder (awaiting XML comments)

### Known Issues
1. **Projection Module**: Currently commented out in generator pipeline
2. **Delete Operations**: Not implemented
3. **Polling Watch**: Placeholder implementation
4. **API Documentation**: XML comments incomplete

## Evolution of Project Decisions

### Decision Log
| Date | Decision | Rationale |
|------|----------|-----------|
| Initial | Use source generators | Avoid runtime reflection overhead |
| Initial | Partial properties | Clean API without boilerplate |
| Initial | BsonDocument for metadata | Flexibility for custom metadata types |
| Current | Incremental generators | Better build performance |
| Current | Modular generator design | Easier to maintain and extend |
| June 2026 | DocFX for documentation | Native .NET documentation tool with GitHub Pages support |
| June 2026 | Root /Docs folder | Centralized documentation for single-repo structure |
| June 2026 | GitHub Actions for deployment | Automated documentation deployment on push |
| June 2026 | Equatable models for caching | Roslyn symbols don't implement value equality, causing cache misses |
| June 2026 | BsonDocument intermediate for nested queries | FilterDefinition<T> can't be converted between different T types |
| June 2026 | Separate ObjectDiscovery and Extension modules | DI registration vs per-class extensions are different concerns |

### Architecture Evolution
1. **Started with**: Basic document mapping
2. **Added**: Change tracking via INotifyPropertyChanged
3. **Added**: Source generation for automatic implementation
4. **Added**: Metadata support with separate types
5. **Added**: Caching and locking for production use
6. **Added**: Documentation site with DocFX and GitHub Pages
7. **Added**: Performance refactoring with equatable models and StringBuilder
8. **Current**: Working on projections and advanced queries

## Testing Strategy

### Manual Testing
The `Progress` project serves as a manual test harness:
- Defines sample documents (`AObject`, `BObject`)
- Demonstrates CRUD operations
- Tests change tracking with nested objects
- Validates locking behavior

### Future Testing Plans
- Unit tests for source generator
- Integration tests with MongoDB
- Performance benchmarks
- Concurrency stress tests

## GitHub Readiness

### Repository Structure ✅
- Root README.md with project overview
- LICENSE.txt at root and in each project
- .github/workflows for CI/CD
- Docs folder for GitHub Pages

### Pending
- [ ] Initial push to GitHub
- [ ] Enable GitHub Pages in repository settings
- [ ] Verify workflow runs successfully
- [ ] Add repository description and topics