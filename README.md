# MongoObject

**A modern MongoDB ODM for .NET 10 with source generation and automatic change tracking.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-239120)](https://learn.microsoft.com/en-us/dotnet/csharp/)

---

## Overview

MongoObject bridges the gap between MongoDB's document model and modern .NET development. Using Roslyn source generators and C# 14 partial properties, it provides an intuitive, EF Core-like experience for working with MongoDB documents.

### Key Features

- **🚀 Source Generation** - Automatic implementation via `[MongoObject]` attribute
- **📊 Change Tracking** - Automatic property change detection for efficient updates
- **📝 Metadata Support** - Separate metadata types for versioning, timestamps, and ownership
- **🔍 Type-Safe Queries** - Generated search classes for compile-time query validation
- **🎯 Projections** - Selective field retrieval with `[ProjectValue]` attribute
- **🔒 Distributed Locking** - Document-level concurrency control
- **⚡ Caching** - Built-in memory caching with configurable expiration
- **👁️ Change Streams** - Real-time MongoDB change monitoring

---

## Installation

> **📦 NuGet Package: Coming Soon**
>
> The `Tenowg.MongoObjects` package is currently in active development and will be published to NuGet once it reaches stability.
>
> In the meantime, you can clone this repository and reference the projects directly.

```bash
# Clone the repository
git clone https://github.com/tenowg/MongoObjects.git

# Add project reference to your .csproj
<ProjectReference Include="path/to/MongoObject.Core/MongoObject.Core.csproj" />
```

---

## Quick Start

### 1. Define Your Document

```csharp
using MongoObject.Core.Attributes;

// Define optional metadata type
public partial record UserMeta
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
}

// Define your document class
[MongoObject(
    CollectionName = "Users",
    DatabaseName = "MyApp",
    MetadataType = typeof(UserMeta)
)]
public partial class User
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }
    public partial int Age { get; set; }
    public partial Address Address { get; set; }
    public partial List<string> Roles { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}
```

### 2. Register in Dependency Injection

```csharp
using MongoObject.Core.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddMongoObject(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "MyApp";
        })
        .AddWatchStream()  // Enable real-time change monitoring
        .RegisterDocumentsFromAssembly();
    });
```

### 3. Use the Document Monitor

```csharp
public class UserService(IDocumentMonitor<User> monitor)
{
    // Create a new document
    public async Task<string> CreateUserAsync(User user)
    {
        return await monitor.Add(user);
    }

    // Get a document by ID
    public async Task<User> GetUserAsync(string id)
    {
        return await monitor.Get(id);
    }

    // Update a document (only changed fields are sent)
    public async Task UpdateUserAsync(User user)
    {
        user.Name = "New Name";  // Change is automatically tracked
        user.Email = "new@email.com";
        await monitor.SaveChanges(user);
    }

    // Lock a document for exclusive access
    public async Task<IDisposable> LockUserAsync(User user)
    {
        return await monitor.LockDocument(user);
    }
}
```

---

## How It Works

### Source Generation

When you decorate a class with `[MongoObject]`, the source generator:

1. **Validates** the class structure at compile time
2. **Generates** partial property implementations with change tracking
3. **Creates** metadata query and record types
4. **Generates** type-safe search classes

### Change Tracking

MongoObject uses `INotifyPropertyChanged` to track property changes:

```csharp
var user = await monitor.Get(userId);
// user is now being tracked

user.Name = "Updated Name";  // Tracked: $set { "Document.Name": "Updated Name" }
user.Age = null;             // Tracked: $unset ["Document.Age"]

// Only changed fields are sent to MongoDB
await monitor.SaveChanges(user);
```

### Document Structure

Documents are wrapped in `MongoDocument<T>`:

```csharp
public class MongoDocument<T>
{
    public string Id { get; set; }           // MongoDB _id
    public T? Document { get; set; }         // Your business data
    public BsonDocument Metadata { get; set; } // Version, timestamps, etc.
}
```

---

## Documentation

Full documentation is available at **[https://tenowg.github.io/MongoObjects](https://tenowg.github.io/MongoObjects)**

### Articles

- [Getting Started](Docs/articles/getting-started.md)
- [Defining Documents](Docs/articles/defining-documents.md)
- [Change Tracking](Docs/articles/change-tracking.md)
- [Metadata](Docs/articles/metadata.md)
- [Searching](Docs/articles/searching.md)
- [Projections](Docs/articles/projections.md)
- [Dependency Injection](Docs/articles/dependency-injection.md)

---

## Project Structure

```
MongoObject/
├── MongoObject.Core/              # Core library
│   ├── Attributes/                # [MongoObject], [ProjectValue]
│   ├── Data/                      # MongoDocument, TrackingObservableObject
│   ├── Interfaces/                # Core interfaces
│   ├── Services/                  # Service implementations
│   └── Extensions/                # DI extensions
├── MongoObject.SourceGenerator/   # Roslyn source generator
│   ├── Generators/                # CommonGenerator
│   └── Modules/                   # Generation modules
├── Docs/                          # Documentation (DocFX)
│   ├── articles/                  # Manual documentation
│   └── api/                       # API reference
└── Progress/                      # Demo/test project
```

---

## Requirements

- **.NET 10 SDK**
- **MongoDB 4.0+** (for change streams support)
- **C# 14** (for partial properties)

---

## Building from Source

```bash
# Debug build
dotnet build

# Release build (generates NuGet package)
dotnet build -c Release

# Run the demo project
dotnet run --project Progress
```

---

## Roadmap

- [ ] Complete projection module implementation
- [ ] Add delete operations
- [ ] Implement polling-based watch mode
- [ ] Add batch operations support
- [ ] Comprehensive unit and integration tests
- [ ] Publish to NuGet

---

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

## Acknowledgments

- Built with [MongoDB.Driver](https://github.com/mongodb/mongo-csharp-driver)
- Uses [Roslyn Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- Versioning with [MinVer](https://github.com/adamralph/minver)