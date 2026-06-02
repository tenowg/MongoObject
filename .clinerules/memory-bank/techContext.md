# Tech Context

## Technologies Used

### Core Framework
- **.NET 10** - Target framework
- **C# 14** - Language version with partial properties, extensions

### Database
- **MongoDB** - Document database
- **MongoDB.Driver 3.8.0** - Official MongoDB driver for .NET

### Code Generation
- **Roslyn Source Generators** - Incremental source generation
- **Microsoft.CodeAnalysis** - Code analysis and generation APIs

### Dependency Injection
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Hosting** - Host builder pattern
- **Microsoft.Extensions.Caching.Memory** - Memory caching

### Documentation
- **DocFX** - Static documentation site generator for .NET
- **GitHub Pages** - Documentation hosting
- **GitHub Actions** - Automated documentation deployment

### Build & Packaging
- **MinVer 7.0.0** - Semantic versioning from git tags
- **NuGet** - Package distribution

### Compression (Internal)
- **SharpCompress 0.48.0** - Compression utilities
- **Snappier 1.3.1** - Snappy compression

## Development Setup

### Project Structure
```
MongoObject/
├── MongoObject.slnx                    # Solution file
├── README.md                           # Root documentation
├── LICENSE.txt                         # MIT License
├── MongoObject.Core/                   # Core library
│   ├── Attributes/                     # [MongoObject], [ProjectValue]
│   ├── Collections/                    # Collection utilities
│   ├── Data/                           # MongoDocument, TrackingObservableObject
│   ├── Exceptions/                     # Custom exceptions
│   ├── Extensions/                     # DI extensions
│   ├── Interfaces/                     # Core interfaces
│   └── Services/                       # Service implementations
├── MongoObject.SourceGenerator/        # Source generator
│   ├── Generators/                     # CommonGenerator
│   ├── Interfaces/                     # Module interfaces
│   ├── Models/                         # Code models
│   └── Modules/                        # Generation modules
├── Docs/                               # Documentation (DocFX)
│   ├── docfx.json                      # DocFX configuration
│   ├── toc.yml                         # Table of contents
│   ├── index.md                        # Documentation home
│   ├── api/                            # API reference (auto-generated)
│   └── articles/                       # Manual documentation
│       ├── getting-started.md
│       ├── defining-documents.md
│       ├── change-tracking.md
│       ├── metadata.md
│       ├── searching.md
│       ├── projections.md
│       └── dependency-injection.md
├── Progress/                           # Test/demo project
└── .github/
    └── workflows/
        └── docs.yml                    # Documentation deployment workflow
```

### Building the Project
```bash
# Debug build
dotnet build

# Release build (generates NuGet package)
dotnet build -c Release

# Run demo project
dotnet run --project Progress
```

### Building Documentation Locally
```bash
# Install DocFX
dotnet tool install -g docfx

# Build documentation
cd Docs
docfx metadata
docfx build

# Serve locally
docfx serve _site
```

### Prerequisites
- .NET 10 SDK
- MongoDB instance (local or remote)
- Connection string configuration in `Progress/Program.cs`
- DocFX (for local documentation builds)

## Technical Constraints

### Source Generator Limitations
- Must target `netstandard2.0` for compatibility
- Cannot reference .NET 10 APIs directly
- Incremental generators required for performance

### MongoDB Considerations
- Change streams require replica set or `directConnection=true`
- Transactions require replica set
- BsonDocument serialization for metadata flexibility

### C# 14 Features Used
- **Partial properties**: For source-generated property implementations
- **Extensions**: New extension method syntax (`extension(IServiceCollection services)`)
- **Primary constructors**: For service classes

### GitHub Pages Requirements
- Repository must have GitHub Pages enabled (Settings → Pages → Source: GitHub Actions)
- Workflow requires `pages: write` and `id-token: write` permissions
- Documentation builds on push to main branch

## Dependencies

### MongoObject.Core Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| MongoDB.Driver | 3.8.0 | MongoDB connectivity |
| SharpCompress | 0.48.0 | Compression |
| Snappier | 1.3.1 | Snappy compression |
| MinVer | 7.0.0 | Versioning |

### MongoObject.SourceGenerator Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.CodeAnalysis.CSharp | 4.x | Source generation |

### Documentation Dependencies
| Tool | Purpose |
|------|---------|
| DocFX | Documentation site generation |
| GitHub Actions | Automated deployment |

## Tool Usage Patterns

### Adding a New Document Type
1. Create partial class with `[MongoObject]` attribute
2. Define partial properties for tracked fields
3. Optionally define metadata type
4. Register with `RegisterDocument<T>()` in DI

### Running the Demo
1. Ensure MongoDB is running
2. Update connection string in `Progress/Program.cs`
3. Run `dotnet run --project Progress`

### Debugging Source Generator
- Uncomment `Debugger.Launch()` in `CommonGenerator.cs`
- Build project to trigger generator
- Attach debugger when prompted

### Updating Documentation
1. Edit markdown files in `Docs/articles/`
2. Update `Docs/articles/toc.yml` if adding new articles
3. Test locally with `docfx build` and `docfx serve _site`
4. Push to main - GitHub Actions will auto-deploy

### Adding New Documentation Articles
1. Create new `.md` file in `Docs/articles/`
2. Add entry to `Docs/articles/toc.yml`
3. Link from `Docs/index.md` if it's a major topic
4. Update root `README.md` documentation links if needed