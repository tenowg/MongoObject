# Projections

MongoObject supports selective field retrieval using the `[ProjectValue]` attribute, allowing you to retrieve only the fields you need.

---

## Overview

Projections in MongoDB allow you to retrieve only specific fields from a document, reducing network traffic and memory usage. MongoObject makes projections type-safe with the `[ProjectValue]` attribute.

---

## The ProjectValue Attribute

Use `[ProjectValue]` to define named projections on your document properties:

```csharp
[MongoObject(CollectionName = "Users")]
public partial class User
{
    [ProjectValue("BasicInfo", ProjectionType.Include)]
    public partial string Name { get; set; }
    
    [ProjectValue("BasicInfo", ProjectionType.Include)]
    public partial string Email { get; set; }
    
    [ProjectValue("BasicInfo", ProjectionType.Exclude)]
    public partial string PasswordHash { get; set; }
    
    public partial int Age { get; set; }  // Not included in BasicInfo
}
```

---

## Projection Types

### Include

The field will be included in the projection:

```csharp
[ProjectValue("MyProjection", ProjectionType.Include)]
public partial string Name { get; set; }
```

### Exclude

The field will be excluded from the projection:

```csharp
[ProjectValue("MyProjection", ProjectionType.Exclude)]
public partial string SensitiveData { get; set; }
```

### Slice

The field will be Sliced in the projection returning a part of an array

```csharp
[ProjectValue("MyProjection", ProjectionType.Slice)]
public partial List<string> Tags { get; set; } = [];
```
---

## Multiple Projections

A property can be part of multiple projections:

```csharp
public partial class User
{
    [ProjectValue("BasicInfo", ProjectionType.Include)]
    [ProjectValue("PublicProfile", ProjectionType.Include)]
    public partial string Name { get; set; }
    
    [ProjectValue("BasicInfo", ProjectionType.Include)]
    [ProjectValue("ContactInfo", ProjectionType.Include)]
    public partial string Email { get; set; }
    
    [ProjectValue("ContactInfo", ProjectionType.Include)]
    public partial string Phone { get; set; }
    
    [ProjectValue("BasicInfo", ProjectionType.Exclude)]
    [ProjectValue("PublicProfile", ProjectionType.Exclude)]
    public partial string PasswordHash { get; set; }
}
```

This creates three projection sets:
- **BasicInfo**: Name, Email (PasswordHash excluded)
- **PublicProfile**: Name (PasswordHash excluded)
- **ContactInfo**: Email, Phone

---

## Using Projections

> [!NOTE] 
> Projection support is currently in development. The API will be finalized in an upcoming release.

### Current API

```csharp
var users = await monitor.Search().WithProjectionBasicInfo();
// Returns users with only Name and Email populated
```

```csharp
var users = await monitor.Search().WithProjectionMyProjection().WithTagsSlice(5, 3);
```

Because Codegen generates a custom Builder for every projection model all queries because fluent and 100% accessible from intellisense

---

## Projection Rules

### MongoDB Projection Behavior

1. **Include Mode**: Only specified fields are returned (plus `_id`)
2. **Exclude Mode**: All fields except excluded ones are returned
3. **Cannot Mix**: You cannot mix include and exclude in the same projection (except for `_id`)

> [!NOTE]
> Exclude does not work as expected yet, as of now it only insures that it is not included along with Include types, this is on the roadmap to be fixed

### How MongoObject Handles This

```csharp
public partial class Document
{
    // Include projection - only these fields
    [ProjectValue("Minimal", ProjectionType.Include)]
    public partial string Title { get; set; }
    
    [ProjectValue("Minimal", ProjectionType.Include)]
    public partial string Summary { get; set; }
    
    // Not in Minimal projection
    public partial string FullContent { get; set; }
}
```

When using "Minimal" projection:
- ✓ Title is returned
- ✓ Summary is returned
- ✗ FullContent is NOT returned

---

## Best Practices

### 1. Define Projections for Common Use Cases

```csharp
public partial class Article
{
    // For list views
    [ProjectValue("ListView", ProjectionType.Include)]
    [ProjectValue("DetailView", ProjectionType.Include)]
    public partial string Title { get; set; }
    
    [ProjectValue("ListView", ProjectionType.Include)]
    [ProjectValue("DetailView", ProjectionType.Include)]
    public partial string Author { get; set; }
    
    [ProjectValue("ListView", ProjectionType.Include)]
    public partial DateTime PublishedAt { get; set; }
    
    // For detail views
    [ProjectValue("DetailView", ProjectionType.Include)]
    public partial string Content { get; set; }
    
    [ProjectValue("DetailView", ProjectionType.Include)]
    public partial List<Comment> Comments { get; set; }
}
```

### 2. Exclude Sensitive Data

```csharp
public partial class User
{
    [ProjectValue("PublicProfile", ProjectionType.Include)]
    public partial string Name { get; set; }
    
    [ProjectValue("PublicProfile", ProjectionType.Include)]
    public partial string Avatar { get; set; }
    
    // Always exclude from public profile
    [ProjectValue("PrivateProfile", ProjectionType.Exclude)]
    public partial string Email { get; set; }
    
    [ProjectValue("PrivateProfile", ProjectionType.Exclude)]
    public partial string PasswordHash { get; set; }
}
```

### 3. Name Projections Descriptively

```csharp
// ✓ Good - descriptive names
[ProjectValue("UserListView", ProjectionType.Include)]
[ProjectValue("UserDetailView", ProjectionType.Include)]
[ProjectValue("AdminReport", ProjectionType.Include)]

// ✗ Avoid - generic names
[ProjectValue("Projection1", ProjectionType.Include)]
[ProjectValue("Small", ProjectionType.Include)]
```

### 4. Code Generator Outputs
Behind the scenes the code generator is generating records for each ProjectValue name:
  - `public record UserPublicProfile { ... included properties...}`
  - `public record UserPrivateProfile { ... included properties ...}`

The generator is also building the Query Builders for each Projection so you can use Fluent patterns to build your queries. Returning a `IEnumerable<UserPublicProfile>`

``` csharp
_monitor.Search()
    .WithQuery(s => s.Name = "Jane Smith")
    .WithMeta(m => m.CreatedAt = m.CreatedAt.Lt(DateTime.UtcNow))
    .WithUserPublicProjection();
```

---

## Example: Blog Application

```csharp
[MongoObject(CollectionName = "Posts", DatabaseName = "Blog")]
public partial class BlogPost
{
    // List view projection
    [ProjectValue("List", ProjectionType.Include)]
    [ProjectValue("Detail", ProjectionType.Include)]
    public partial string Title { get; set; }
    
    [ProjectValue("List", ProjectionType.Include)]
    public partial string Excerpt { get; set; }
    
    [ProjectValue("List", ProjectionType.Include)]
    [ProjectValue("Detail", ProjectionType.Include)]
    public partial string AuthorName { get; set; }
    
    [ProjectValue("List", ProjectionType.Include)]
    [ProjectValue("Detail", ProjectionType.Include)]
    public partial DateTime PublishedAt { get; set; }
    
    [ProjectValue("List", ProjectionType.Include)]
    public partial List<string> Tags { get; set; }
    
    [ProjectValue("Detail", ProjectionType.Include)]
    public partial string FullContent { get; set; }
    
    [ProjectValue("Detail", ProjectionType.Include)]
    public partial List<Comment> Comments { get; set; }
    
    // Never expose internal fields
    [ProjectValue("List", ProjectionType.Exclude)]
    [ProjectValue("Detail", ProjectionType.Exclude)]
    public partial string InternalNotes { get; set; }
}

public class BlogService
{
    private IDocumentMonitor<BlogPost> _monitor;

    public BlogService(IDocumentMonitor<BlogPost> monitor)
    {
        _monitor = monitor;
    }

    public async Task<IEnumerable<BlogPostList>> GetPostList()
    {
        // Will use "List" projection for efficient data retrieval
        var blogs = await _monitor.Search()
            .Limit(10)
            .Skip(10)
            .WithListProjection();

        return blogs;
    }
    
    public async Task<BlogPostDetail> GetPostDetail(string id)
    {
        // Will use "Detail" projection
        var blogs = await _monitor.Search()
            .Limit(10)
            .Skip(10)
            .WithDetailProjection();
        return blogs;
    }
}
```

---

## Next Steps

- **[Defining Documents](defining-documents.md)** - Learn more about document attributes
- **[Searching](searching.md)** - Query documents efficiently
- **[Change Tracking](change-tracking.md)** - Understand how projections interact with updates