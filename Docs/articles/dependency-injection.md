# Dependency Injection

MongoObject is designed to work seamlessly with .NET's built-in dependency injection container. This guide covers all configuration options and registration patterns.

---

## Basic Setup

### 1. Add MongoObject Services

```csharp
using MongoObject.Core.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddMongoObject(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "MyDatabase";
        });
    });
```

### 2. Register Documents

```csharp
services.AddMongoObject(options => { ... })
    .RegisterDocumentsFromAssembly();  // Register all [MongoObject] classes in the assembly
```

NOTE: `FromAssembly` refers to the name of you Assembly. e.g. If your project is named Progress, then the full name is `.RegisterDocumentsProgress`

Or register specific documents (Highly un-recommended, use the code gened Extension):

```csharp
services.AddMongoObject(options => { ... })
    .RegisterDocument<User, UserMetaQuery, UserMetaRecord>();
```

---

## Configuration Options

The `MongoObjectOptions` class provides these settings:

```csharp
services.AddMongoObject(options =>
{
    // Required: MongoDB connection string
    options.ConnectionString = "mongodb://localhost:27017";
    
    // Required: Default database name
    options.DatabaseName = "MyDatabase";
    
    // Optional: Cache settings
    options.CacheAbsoluteExpiration = TimeSpan.FromMinutes(30);
    options.CacheSlidingExpiration = TimeSpan.FromMinutes(5);
    
    // Optional: Enable distributed locking
    options.EnableDistributedLocking = true;
});
```

### Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | `string` | Required | MongoDB connection string |
| `DatabaseName` | `string` | Required | Default database name |
| `CacheAbsoluteExpiration` | `TimeSpan?` | `null` | Absolute cache expiration time |
| `CacheSlidingExpiration` | `TimeSpan?` | `null` | Sliding cache expiration time |
| `EnableDistributedLocking` | `bool` | `false` | Enable distributed lock support |

---

## Document Registration

### Register All Documents from Assembly

```csharp
services.AddMongoObject(options => { ... })
    .RegisterDocumentsFromAssembly();
```

This scans the assembly for all classes with `[MongoObject]` and registers:
- `IDocumentMonitor<T>` for each document type

### Register Specific Documents(not recommended)

```csharp
services.AddMongoObject(options => { ... })
    .RegisterDocument<User, UserMetaQuery, UserMetaRecord>()
    .RegisterDocument<Product, ProductMetaQuery, ProductMetaRecord>();
```

---

## Watch Modes

MongoObject supports real-time change monitoring through watch modes.

### Stream-Based Watch (Recommended)

Uses MongoDB change streams for real-time updates:

```csharp
services.AddMongoObject(options => { ... })
    .AddWatchStream()  // Enable change stream monitoring
    .RegisterDocumentsFromAssembly();
```

**Requirements:**
- MongoDB replica set or `directConnection=true` for standalone
- MongoDB 4.0+

**Connection string for local development:**
```csharp
options.ConnectionString = "mongodb://localhost:27017/?directConnection=true";
```

### Polling-Based Watch (Coming Soon)

For environments where change streams aren't available:

```csharp
// Future feature
services.AddMongoObject(options => { ... })
    .AddWatchPolling(TimeSpan.FromSeconds(5));  // Poll every 5 seconds
```

---

## Using Registered Services

### Inject IDocumentMonitor<T>

```csharp
public class UserService
{
    private readonly IDocumentMonitor<User> _monitor;

    public UserService(IDocumentMonitor<User> monitor)
    {
        _monitor = monitor;
    }

    public async Task<User> GetUser(string id) => await _monitor.Get(id);
    public async Task<string> CreateUser(User user) => await _monitor.Add(user);
    public async Task UpdateUser(User user) => await _monitor.SaveChanges(user);
}
```

---

## Complete Example

```csharp
using Microsoft.Extensions.Hosting;
using MongoObject.Core.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure MongoObject
        services.AddMongoObject(options =>
        {
            // Get connection string from configuration
            options.ConnectionString = context.Configuration.GetConnectionString("MongoDB") 
                ?? "mongodb://localhost:27017";
            options.DatabaseName = "MyApplication";
            
            // Enable caching
            options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
            options.CacheSlidingExpiration = TimeSpan.FromMinutes(10);
        })
        .AddWatchStream()  // Enable real-time monitoring
        .RegisterDocumentsFromAssembly();  // Register all documents
        
        // Register application services
        services.AddScoped<UserService>();
        services.AddScoped<UserSearchService>();
        services.AddScoped<ArticleService>();
    });

var host = builder.Build();
await host.RunAsync();
```

---

## appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/?directConnection=true"
  },
  "MongoObject": {
    "DatabaseName": "MyApplication",
    "CacheAbsoluteExpirationMinutes": 60,
    "CacheSlidingExpirationMinutes": 10
  }
}
```

Then use configuration binding:

```csharp
services.AddMongoObject(options =>
{
    options.ConnectionString = configuration.GetConnectionString("MongoDB");
    options.DatabaseName = configuration["MongoObject:DatabaseName"];
});
```

---

## Service Lifetimes

MongoObject services are registered with these lifetimes:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IDocumentMonitor<T>` | Singleton | One per request/scope |
| `IMongoConnection<T>` | Singleton | MongoDB connection wrapper |
| `InternalCacheService` | Singleton | Shared cache across requests |
| `DistributedLockManager` | Singleton | Lock coordination |

---

## Best Practices

### 1. Use Scoped Services in Web Applications

```csharp
// ✓ Good - scoped services work well with web requests
services.AddScoped<OrderService>();

public class OrderService
{
    private readonly IDocumentMonitor<Order> _monitor;
    // Works correctly within a request scope
}
```

### 2. Don't Share Monitors Across Threads

```csharp
// ✗ Bad - monitors are not thread-safe
private static IDocumentMonitor<User> _sharedMonitor;

// ✓ Good - inject per scope
public class UserService
{
    private readonly IDocumentMonitor<User> _monitor;
    public UserService(IDocumentMonitor<User> monitor) => _monitor = monitor;
}
```

### 3. Configure Caching Appropriately

```csharp
// For frequently changing data - shorter expiration
options.CacheAbsoluteExpiration = TimeSpan.FromMinutes(5);

// For relatively static data - longer expiration
options.CacheAbsoluteExpiration = TimeSpan.FromHours(1);
```

### 4. Use Distributed Locking for Concurrent Access

```csharp
options.EnableDistributedLocking = true;

// Then use locks when modifying documents
await using var lockScope = await monitor.LockDocument(document);
document.Balance += 100;
await monitor.SaveChanges(document, lockScope);
```

---

## Next Steps

- **[Getting Started](getting-started.md)** - Review the basics
- **[Change Tracking](change-tracking.md)** - Understand how tracking works with DI
- **[Searching](searching.md)** - Use search services effectively