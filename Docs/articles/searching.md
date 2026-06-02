# Searching

MongoObject provides type-safe search capabilities through auto-generated search classes. Query documents and metadata without writing raw MongoDB filters.

---

## Overview

When you define a document, MongoObject generates search classes that provide type-safe query building:

```csharp
[MongoObject(CollectionName = "Users")]
public partial class User
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }
    public partial int Age { get; set; }
}
```

This generates a `UserSearch` class for querying documents.

---

## Document Search

Use `DocumentSearch` to find documents by their properties:

```csharp
// Find users by name
var users = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Name = "John";
});

// Find users by email domain
var gmailUsers = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Email = "@gmail.com";  // Contains match
});

// Find users by age
var adults = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Age = 18;  // Exact match for value types
});
```

---

## Metadata Search

Use `MetadataSearch` to find documents by their metadata:

```csharp
// Find documents by department
var engineering = await monitor.MetadataSearch<UserMetaQuery>(meta =>
{
    meta.Department = "Engineering";
});

// Find documents created by a user
var myDocs = await monitor.MetadataSearch<UserMetaQuery>(meta =>
{
    meta.CreatedBy = currentUser.Id;
});

// Find recent documents
var recent = await monitor.MetadataSearch<UserMetaQuery>(meta =>
{
    meta.LastModifiedAt = DateTime.UtcNow.AddDays(-7);  // Modified in last 7 days
});
```

---

## Generated Search Classes

### Document Search Class

For each document type, a search class is generated:

```csharp
// Auto-generated
public class UserSearch : IClassSearch<User>
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
}
```

### Metadata Search Class

If you specify a `MetadataType`, a metadata search class is generated:

```csharp
// Auto-generated
public class UserMetaQuery : IMetadataSearchBase
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
```

---

## Search Behavior

### String Properties

String properties perform substring matches:

```csharp
search.Name = "John";  // Matches "John", "Johnny", "Elton John"
```

### Value Types

Value types perform exact matches:

```csharp
search.Age = 25;  // Matches only Age == 25
```

### Nullable Types

Setting a nullable to null searches for documents where that field is not set:

```csharp
search.Birthday = null;  // Matches documents without Birthday
```

---

## Combining Searches

All specified search criteria are combined with AND logic:

```csharp
var results = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Name = "John";      // Name contains "John"
    search.Age = 25;           // AND Age equals 25
});
// Matches users named John who are 25 years old
```

---

## Complete Example

```csharp
public class UserSearchService
{
    private readonly IDocumentMonitor<User> _monitor;
    private readonly IDocumentMonitorInternal<User> _monitorInternal;

    public UserSearchService(IDocumentMonitor<User> monitor, IDocumentMonitorInternal<User> monitorInternal)
    {
        _monitor = monitor;
        _monitorInternal = monitorInternal;
    }

    // Search by document properties
    public async Task<IEnumerable<User>> FindUsersByName(string name)
    {
        return await _monitorInternal.DocumentSearch<UserSearch>(search =>
        {
            search.Name = name;
        });
    }

    // Search by metadata
    public async Task<IEnumerable<User>> FindUsersByDepartment(string department)
    {
        return await _monitorInternal.MetadataSearch<UserMetaQuery>(meta =>
        {
            meta.Department = department;
        });
    }

    // Combined search
    public async Task<IEnumerable<User>> FindActiveAdmins()
    {
        // Search by document property
        var admins = await _monitorInternal.DocumentSearch<UserSearch>(search =>
        {
            search.Roles = "admin";  // Has admin role
        });

        // Further filter by metadata
        return await _monitorInternal.MetadataSearch<UserMetaQuery>(meta =>
        {
            meta.Status = UserStatus.Active;
        });
    }
}
```

---

## Advanced Queries

For complex queries beyond what the search classes provide, you can access the underlying MongoDB collection:

```csharp
// Note: This is a future feature
// For now, use the search classes for type-safe queries
```

---

## Best Practices

### 1. Use Specific Search Criteria

```csharp
// ✓ Good - specific search
var users = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Email = "john@example.com";
});

// ✗ Avoid - too broad
var users = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Name = "a";  // Too many results
});
```

### 2. Combine Document and Metadata Searches

```csharp
// First filter by document properties
var candidates = await monitor.DocumentSearch<UserSearch>(search =>
{
    search.Department = "Engineering";
});

// Then filter by metadata
var recentHires = await monitor.MetadataSearch<UserMetaQuery>(meta =>
{
    meta.CreatedAt = DateTime.UtcNow.AddMonths(-3);
});
```

### 3. Cache Frequently Used Queries

```csharp
private IEnumerable<User>? _activeAdmins;
private DateTime _lastFetch;

public async Task<IEnumerable<User>> GetActiveAdmins()
{
    if (_activeAdmins == null || DateTime.UtcNow - _lastFetch > TimeSpan.FromMinutes(5))
    {
        _activeAdmins = await monitor.DocumentSearch<UserSearch>(search =>
        {
            search.Roles = "admin";
        });
        _lastFetch = DateTime.UtcNow;
    }
    return _activeAdmins;
}
```

---

## Next Steps

- **[Projections](projections.md)** - Learn about selective field retrieval
- **[Metadata](metadata.md)** - Understand metadata search in depth
- **[Change Tracking](change-tracking.md)** - Track changes to searched documents