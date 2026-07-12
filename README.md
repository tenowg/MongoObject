# MongoObject

**A modern MongoDB ODM for .NET 10 with source generation and automatic change tracking.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-239120)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Deploy Documentation](https://github.com/tenowg/MongoObject/actions/workflows/docs.yml/badge.svg)](https://github.com/tenowg/MongoObject/actions/workflows/docs.yml)
[![Build](https://github.com/tenowg/MongoObject/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tenowg/MongoObject/actions/workflows/dotnet.yml)
[![NuGet Pre-release Version](https://shields.io/nuget/vpre/Tenowg.MongoObjects)](https://www.nuget.org/packages/Tenowg.MongoObjects)

---

## Overview

MongoObject is a source-generated MongoDB ODM for modern .NET.

Instead of writing string-based queries, update definitions, projection definitions, indexes, and validation schemas manually, MongoObject generates strongly typed APIs directly from your POCO models at compile time.

Resulting in:

- Compile-time validation instead of runtime errors
- Automatic change tracking
- Minimal update payloads
- Zero reflection-based mapping
- Less boilerplate than using MongoDB.Driver directly

MongoObject bridges the gap between MongoDB's document model and modern .NET development. Using Roslyn source generators and C# 14 partial properties, it provides an intuitive, inspired by EF Core's change tracking, experience for working with MongoDB documents.

### Why

I think this explains it all:

> "MongoObject wasn't built because MongoDB.Driver is bad. It was built because even experienced MongoDB developers have to stop and think every time they write a `Builders<T>.Filter` query. The API is powerful but not discoverable. MongoObject makes the right thing the obvious thing."

### Key Features

- **🚀 Source Generation** - Automatic implementation via `[MongoObject]` attribute
- **📊 Change Tracking** - Automatic property change detection for efficient updates
- **📝 Metadata Support** - Separate metadata types for versioning, timestamps, and ownership
- **🔍 Type-Safe Queries** - Generated search classes for compile-time query validation
- **🎯 Projections** - Selective field retrieval with `[ProjectValue]` attribute
- **🔒 Distributed Locking** - Document-level concurrency control
- **⚡ Caching** - Built-in memory caching with configurable expiration
- **👁️ Change Streams** - Real-time MongoDB change monitoring
- **Cli Based Migration Support** - run a migration tool to help build, validate, and rename properties
- **Vector Search Ingration** - Vector Search is a built in feature, enabled by adding a few Attributes, described more in the full documentation.

---

### Design Goals
MongoObject is designed around a few principles:

- Compile-time safety over runtime configuration
- POCO-first development
- Minimal MongoDB payloads
- Zero boilerplate
- Source generation instead of reflection
- Native MongoDB features without hiding MongoDB itself

---

## Installation

> The `Tenowg.MongoObjects` package is currently in active development.

```bash
// install the main package
dotnet add package Tenowg.MongoObjects --prerelease

// install the Cli
dotnet tool install -g Tenowg.MongoObjects.CliTool
```
OR
```bash
# Clone the repository
git clone https://github.com/tenowg/MongoObject.git

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
    .ConfigureServices(config, (_, services) =>
    {
        services.AddMongoObject(config, (builder, options) =>
        {
            options.ConnectionString = apiKey;
            options.DatabaseName = "mydatabase";

            builder.AddWatchStream()
            .RegisterDocumentsProjectName();
        });
    });
```

> RegisterDocumentsProjectName is a source generated discovery class, calling it will register all 
> POCO class marked with `[MongoObject]` with DI. Just need to change ProjectName to your `ProjectName`. This will also handle
> POCO's defined in other libraries such as shared data libraries. Just need to add an additional
> .RegisterDocumentsSharedLibrary()

### 3. Use the Document Monitor

#### Document Updates

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

#### Query

MongoObject uses a custom Fluent Query API instead of LINQ for better performance, compile-time metadata support, and Native AOT compatibility.

``` csharp
var results = await monitor.Search()
    .WithQuery(f =>
    {
        f.Name = "Case";
        f.Age = f.Age.And(
            f.Age.Lt(40000),
            f.Age.Gt(5)
        );
        f.test(t =>
        {
            t.Name = "Andrew";
        });
    })
    .WithMeta(meta =>
    {
        meta.LastModifiedAt = meta.LastModifiedAt.Lt(DateTime.UtcNow);
    });
```

Compared with MongoDB.Driver

```csharp
var collection = database.GetCollection<MongoDocument<User>>("Users");

var filter = Builders<MongoDocument<User>>.Filter.And(
    // Document fields
    Builders<MongoDocument<User>>.Filter.Eq(x => x.Document.Name, "Case"),
    Builders<MongoDocument<User>>.Filter.And(
        Builders<MongoDocument<User>>.Filter.Lt(x => x.Document.Age, 40000),
        Builders<MongoDocument<User>>.Filter.Gt(x => x.Document.Age, 5)
    ),
    // Nested MongoObject query
    Builders<MongoDocument<User>>.Filter.Eq(x => x.Document.Test.Name, "Andrew"),
    // Metadata field
    Builders<MongoDocument<User>>.Filter.Lt(
        x => x.Metadata["LastModifiedAt"].AsDateTime, DateTime.UtcNow
    )
);

var results = await collection.Find(filter).ToListAsync();

```

#### Projections

Define projections directly on your properties with attributes:

```csharp
[ProjectValue("Summary", ProjectionType.Include)]
public partial string Name { get; set; }

[ProjectValue("Summary", ProjectionType.Include)]
public partial int Age { get; set; }
```

MongoObject generates a strongly typed projection record at compile time:

```csharp
var summary = await monitor.Search()
    .WithQuery(f => f.Name = "Case")
    .WithSummaryProjection();

Console.WriteLine(summary.First().Name); // strongly typed result
```

No ProjectionDefinition<T>, no field name strings, no manual serializer wiring.

---

### 4. Use the cli
The optional CLI keeps your MongoDB schema synchronized with your codebase by generating validation schemas, tracking renamed properties, and assisting with safe document migrations. (optional, By use of `[MigrationSchema(OrphanFieldPolicy.AlwaysAsk)]`)

``` csharp
mo migrate build -p ../Path/To/Project
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

Full documentation is available at **[https://tenowg.github.io/MongoObject](https://tenowg.github.io/MongoObject)**

### Articles

- [Getting Started](Docs/articles/getting-started.md)
- [Defining Documents](Docs/articles/defining-documents.md)
- [Change Tracking](Docs/articles/change-tracking.md)
- [Metadata](Docs/articles/metadata.md)
- [Searching](Docs/articles/searching.md)
- [Projections](Docs/articles/projections.md)
- [Dependency Injection](Docs/articles/dependency-injection.md)

---

## Requirements

- **.NET 10 SDK**
- **MongoDB 4.0+** (for change streams support)
- **C# 14** (for partial properties)

MongoObject targets .NET 10 because it relies on C# 14 partial properties to generate strongly typed document implementations without runtime proxies or reflection.

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

For the best idea of what is coming or what is on the Roadmap please look at current Issues.

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