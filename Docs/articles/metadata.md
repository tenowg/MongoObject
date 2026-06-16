# Metadata

MongoObject supports custom metadata types for storing document-level information separate from your business data, such as versioning, timestamps, and ownership.

---

## Overview

Metadata in MongoObject is stored separately from your document data in the `MongoDocument<T>` wrapper:

```csharp
public class MongoDocument<T>
{
    public string Id { get; set; }           // MongoDB _id
    public T? Document { get; set; }         // Your business data
    public BsonDocument Metadata { get; set; } // Metadata (version, timestamps, etc.)
}
```

---

## Defining a Metadata Type

Create a partial record to define your metadata fields:

```csharp
public partial record UserMeta
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }
}
```

Then associate it with your document using the `MetadataType` property:

```csharp
[MongoObject(
    CollectionName = "Users",
    DatabaseName = "MyApp",
    MetadataType = typeof(UserMeta)
)]
public partial class User
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }
}
```

---

## Automatic Metadata Fields

MongoObject automatically manages certain metadata fields:

### Version

Every document has an auto-incrementing version number:

```javascript
// Automatically updated on each SaveChanges()
{
  "Metadata": {
    "Version": 5  // Incremented automatically
  }
}
```

The update pipeline includes:
```javascript
"Metadata.Version": { $add: [{ $ifNull: ["$Metadata.Version", 0] }, 1] }
```

### LastModifiedAt

Timestamp of the last modification:

```javascript
// Automatically set using MongoDB's $$NOW
{
  "Metadata": {
    "LastModifiedAt": ISODate("2026-01-15T10:30:00Z")
  }
}
```

### CreatedAt

Timestamp at the time of Creation
``` javascript
{ 
  "Metadata": {
    "CreatedAt": ISODate("2026-01-15T10:30:00Z")
  }
}
```

---

## Generated Metadata Types

When you specify a `MetadataType`, the source generator creates:

### 1. Metadata Query Type

For searching documents by metadata:

```csharp
// Auto-generated
public class UserMetaQuery : IMetadataSearchBase
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int? LoginCount { get; set; }
}
```

### 2. Metadata Record Type

For storing metadata values:

```csharp
// Auto-generated
public class UserMetaRecord : IMetadataBase
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }
}
```

---

## Setting Metadata on Creation

When adding a new document, you can set initial metadata values:

```csharp
var user = new User
{
    Name = "John Doe",
    Email = "john@example.com"
};

// Add with metadata
await monitor.Add(user, metadata =>
{
    metadata.CreatedBy = "admin";
    metadata.Department = "Engineering";
});
```

---

## Querying by Metadata

Use the generated metadata query type to search documents:

```csharp
// Find all users in the Engineering department
var users = await monitor.MetadataSearch<UserMetaQuery>(meta =>
{
    meta.Department = "Engineering";
});

// Find documents created by a specific user
var myDocs = await monitor.MetadataSearch<UserMetaQuery>(meta =>
{
    meta.CreatedBy = currentUser.Id;
});
```

---

## Metadata Best Practices

### 1. Keep Metadata Simple

Metadata should contain simple, serializable values:

```csharp
// ✓ Good - simple types
public partial record DocumentMeta
{
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? OwnerId { get; set; }
    public DocumentStatus Status { get; set; }  // Enum is fine
}

// ✗ Avoid - complex nested objects
public partial record DocumentMeta
{
    public List<AuditEntry> AuditTrail { get; set; }  // Too complex
    public Dictionary<string, object> CustomData { get; set; }  // Not type-safe
}
```

### 2. Use Metadata for Cross-Cutting Concerns

```csharp
public partial record AuditableMeta
{
    // Ownership
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Multi-tenancy
    public string? TenantId { get; set; }
}
```

### 3. Don't Duplicate Document Data

```csharp
// ✗ Bad - duplicating document data in metadata
public partial record UserMeta
{
    public string? UserName { get; set; }  // Already in User.Name
}

// ✓ Good - metadata contains supplementary info
public partial record UserMeta
{
    public string? CreatedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

---

## Example: Complete Metadata Setup

```csharp
// 1. Define metadata type
public partial record ArticleMeta
{
    public string? AuthorId { get; set; }
    public string? Category { get; set; }
    public ArticleStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
}

public enum ArticleStatus
{
    Draft,
    Review,
    Published,
    Archived
}

// 2. Define document with metadata
[MongoObject(
    CollectionName = "Articles",
    DatabaseName = "Blog",
    MetadataType = typeof(ArticleMeta)
)]
public partial class Article
{
    public partial string Title { get; set; }
    public partial string Content { get; set; }
    public partial List<string> Tags { get; set; }
}

// 3. Use in service
public class ArticleService
{
    private readonly IDocumentMonitor<Article> _monitor;
    private readonly IDocumentMonitorInternal<Article> _monitorInternal;

    public async Task PublishArticle(string articleId, string publisherId)
    {
        var article = await _monitor.Get(articleId);
        
        // Update document
        article.Status = ArticleStatus.Published;
        
        // Save with metadata update
        await _monitorInternal.Add(article, meta =>
        {
            meta.Status = ArticleStatus.Published;
            meta.PublishedAt = DateTime.UtcNow;
            meta.ModifiedBy = publisherId;
        });
    }

    public async Task<IEnumerable<Article>> GetPublishedArticles()
    {
        return await _monitorInternal.MetadataSearch<ArticleMetaQuery>(meta =>
        {
            meta.Status = ArticleStatus.Published;
        });
    }
}
```

---

## Next Steps

- **[Searching](xref:searching)** - Learn about document and metadata search
- **[Change Tracking](change-tracking.md)** - Understand how metadata versioning works
- **[Dependency Injection](dependency-injection.md)** - Configure metadata options