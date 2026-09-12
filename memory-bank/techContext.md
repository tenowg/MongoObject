# Tech Context: MongoObject

## Target Platform

- **Runtime:** .NET 10.0
- **Language:** C# 14 (partial properties required)
- **Platform:** Cross-platform (Windows, Linux, macOS)
- **AOT:** Compatible (`<IsAotCompatible>true</IsAotCompatible>`)

## Core Dependencies

### Runtime Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| MongoDB.Driver | 3.8.0 | Native MongoDB driver |
| SharpCompress | 0.48.0 | Compression utilities |
| Snappier | 1.3.1 | Snappy compression |
| System.Security.Cryptography.ProtectedData | 10.0.9 | Data protection APIs |

### Build/Generator Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.CodeAnalysis.CSharp | 5.3.0 | Roslyn source generator API |
| Microsoft.CodeAnalysis.Common | 5.3.0 | Roslyn shared types |
| Microsoft.Bcl.Memory | 10.0.8 | Memory utilities for .NET 10 |
| MinVer | 7.0.0 | Git tag-based versioning |

## Development Setup

### Prerequisites
1. **.NET 10 SDK** — Required for C# 14 partial properties support
2. **IDE:** Visual Studio 2022 (latest preview), JetBrains Rider, or VS Code with C# Dev Kit
3. **MongoDB** — Local instance for integration tests (`docker run --name mongoobject-db -p 27017:27017 -d mongodb/mongodb-community-server:latest`)

### Build Commands
```bash
# Debug build
dotnet build

# Release build (generates NuGet package)
dotnet build -c Release

# Run tests
dotnet test

# Run demo/example project
dotnet run --project Examples/ConsoleSetup/ConsoleSetup
dotnet run --project Examples/ExampleWebApi/ExampleWebApi
```

### Solution Structure
```
MongoObject.slnx (solution matrix)
├── MongoObject.Core              → Main library (Tenowg.MongoObjects)
├── MongoObject.SourceGenerator   → Roslyn source generator (netstandard2.0)
├── MongoObject.CliTool           → CLI tool (Tenowg.MongoObjects.CliTool)
├── MongoObject.MongoDistributedLock  → MongoDB distributed lock
├── MongoObject.PropertyEncryption    → Property encryption
├── MongoObject.RedisDistributedLock  → Redis distributed lock
├── MongoObject.SourceGenerator.Encryption → Encryption generator
├── MongoObject.Templates           → Project templates
└── MongoObject.Tests             → Unit & integration tests
```

## NuGet Package Output

All packages output to `nupkg/` directory:
| Package | Latest Version | Description |
|---------|---------------|-------------|
| Tenowg.MongoObjects | 0.2.0-alpha.9 | Main ODM library |
| Tenowg.MongoObjects.CliTool | 0.3.0-beta.1 | CLI migration tool |
| Tenowg.MongoObjects.MongoDistributedLock | 0.2.0-alpha.9 | MongoDB distributed lock |
| Tenowg.MongoObjects.PropertyEncryption | 0.2.0-alpha.9 | Property encryption |
| Tenowg.MongoObjects.RedisDistributedLock | 0.2.0-alpha.9 | Redis distributed lock |
| Tenowg.MongoObjects.Templates | 0.2.0-alpha.9 | Project templates |

## Documentation

- **DocFX** — Used for API documentation generation
- **Published Site:** https://tenowg.github.io/MongoObject
- **Source Location:** `Docs/` directory
- **Articles:** getting-started, defining-documents, change-tracking, metadata, searching, projections, dependency-injection, vector search

## CI/CD

- **GitHub Actions** — Build and documentation deployment workflows
- **Badge Links:**
  - [Build Status](https://github.com/tenowg/MongoObject/actions/workflows/dotnet.yml)
  - [Deploy Documentation](https://github.com/tenowg/MongoObject/actions/workflows/docs.yml)

## Coding Standards

- Standard C# coding conventions
- `.editorconfig` present for IDE consistency
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- InternalsVisibleTo used for test and related project access

## Debugging Source Generators

Debugging Roslyn source generators requires special attention:
1. Use Visual Studio's source generator output viewer
2. Ensure tests validate generated syntax trees, not just runtime behavior
3. Be aware that generated code errors appear as compiler warnings/errors in the consuming project
