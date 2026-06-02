# MongoObject Documentation

Welcome to the official documentation for **MongoObject** - a modern MongoDB ODM for .NET 10 with source generation and automatic change tracking.

---

## What is MongoObject?

MongoObject is a MongoDB Object Document Mapper (ODM) that provides an EF Core-like experience for working with MongoDB documents. It leverages C# 14 partial properties and Roslyn source generators to deliver:

- **Zero boilerplate** document definitions
- **Automatic change tracking** for efficient updates
- **Type-safe queries** with compile-time validation
- **Built-in caching** and distributed locking

---

## Getting Started

If you're new to MongoObject, start here:

1. **[Getting Started](articles/getting-started.md)** - Installation and first steps
2. **[Defining Documents](articles/defining-documents.md)** - Learn about the `[MongoObject]` attribute
3. **[Change Tracking](articles/change-tracking.md)** - Understand how changes are tracked

---

## Documentation Sections

### Articles

| Article | Description |
|---------|-------------|
| [Getting Started](articles/getting-started.md) | Installation, setup, and your first document |
| [Defining Documents](articles/defining-documents.md) | The `[MongoObject]` attribute and partial properties |
| [Change Tracking](articles/change-tracking.md) | How automatic change tracking works |
| [Metadata](articles/metadata.md) | Custom metadata types for versioning and timestamps |
| [Searching](articles/searching.md) | Type-safe document and metadata queries |
| [Projections](articles/projections.md) | Selective field retrieval with `[ProjectValue]` |
| [Dependency Injection](articles/dependency-injection.md) | Configuring MongoObject in your application |

### API Reference

The [API Reference](api/index.md) contains auto-generated documentation for all public types in MongoObject.Core.

> **Note:** API documentation is currently being expanded. Check back for updates.

---

## Quick Example

```csharp
// 1. Define your document
[MongoObject(CollectionName = "Users", DatabaseName = "MyApp")]
public partial class User
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }
    public partial int Age { get; set; }
}

// 2. Register in DI
services.AddMongoObject(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "MyApp";
})
.RegisterDocumentsFromAssembly();

// 3. Use the document monitor
public class UserService(IDocumentMonitor<User> monitor)
{
    public async Task UpdateUserEmail(string userId, string newEmail)
    {
        var user = await monitor.Get(userId);
        user.Email = newEmail;  // Change is automatically tracked
        await monitor.SaveChanges(user);  // Only changed fields are sent
    }
}
```

---

## Requirements

- **.NET 10 SDK**
- **MongoDB 4.0+** (for change streams support)
- **C# 14** (for partial properties)

---

## Contributing

Found a bug or want to contribute? Visit our [GitHub repository](https://github.com/tenowg/MongoObjects).

---

## License

MongoObject is licensed under the MIT License.