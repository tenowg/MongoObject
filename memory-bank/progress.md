# Progress: MongoObject

## What Works

### Core Features
- ✅ Source generation via `[MongoObject]` attribute
- ✅ Automatic property change detection and tracking
- ✅ Type-safe query builders with compile-time validation
- ✅ Selective field projections with `[ProjectValue]`
- ✅ Document wrapper (`MongoDocument<T>`) with metadata support
- ✅ `MongoDocumentManager<T>` for CRUD operations
- ✅ Built-in memory caching with configurable expiration
- ✅ Change stream monitoring

### Extension Features
- ✅ MongoDB-based distributed locking
- ✅ Redis-based distributed locking
- ✅ Property-level encryption (with source generator)
- ✅ CLI migration tool (`mo migrate build`)
- ✅ Schema validation and property rename tracking
- ✅ Vector search integration via attributes

### Infrastructure
- ✅ AoT compatible builds
- ✅ MinVer git tag-based versioning
- ✅ DocFX documentation site (published to GitHub Pages)
- ✅ CI/CD pipelines (build + docs deployment)
- ✅ NuGet package publishing (pre-release)

## Current Status

**Active Branch:** `fix-tracking`  
**Status:** In development — property change tracking improvements

### Recent Releases
| Tag | Description |
|-----|-------------|
| 0.3.5-beta | Latest release tag |
| 0.3.4-beta | Previous release tag |

### Published NuGet Packages (Pre-release)
- Tenowg.MongoObjects: **0.2.0-alpha.9**
- Tenowg.MongoObjects.CliTool: **0.3.0-beta.1**
- Tenowg.MongoObjects.MongoDistributedLock: **0.2.0-alpha.9**
- Tenowg.MongoObjects.PropertyEncryption: **0.2.0-alpha.9**
- Tenowg.MongoObjects.RedisDistributedLock: **0.2.0-alpha.9**
- Tenowg.MongoObjects.Templates: **0.2.0-alpha.9**

## What's Left to Build

Based on active development and roadmap discussion:

1. **Stabilize change tracking** — Current `fix-tracking` branch addresses reliability improvements for property change detection
2. **Generator refactor** — `generator-refactor-projections` branch exists for projection-related improvements
3. **Full documentation coverage** — Additional articles needed for advanced topics
4. **Production-ready release** — Moving from alpha/beta to stable 1.0

## Known Issues & Considerations

- Source generator debugging is complex; always validate generated output in tests
- C# 14 partial properties require .NET 10 SDK and preview tooling
- MongoDB 4.0+ required for change streams support
- String-based field names still appear in some MongoDB.Driver integration points
- NuGet packages are pre-release; API may change between versions

## Evolution of Project Decisions

| Decision | Rationale |
|----------|-----------|
| Target .NET 10 / C# 14 | Required for partial properties feature, essential for the source generation approach |
| Roslyn Source Generators over reflection | AoT compatibility, compile-time safety, zero runtime overhead |
| POCO-first design | No base class inheritance required; works with existing classes |
| MinVer versioning | Git tag-based, aligns with semantic versioning workflow |
| MongoDB.Driver direct dependency | Preserve native feature access rather than abstracting it away |
