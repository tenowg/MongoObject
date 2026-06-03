# Defining Documents

Learn how to define MongoDB documents using the `[MongoObject]` attribute and partial properties.

---

## The MongoObject Attribute

The `[MongoObject]` attribute marks a class as a MongoDB document and configures its behavior:

```csharp
[MongoObject(
    CollectionName = "Users",
    DatabaseName = "MyApp",
    MetadataType = typeof(UserMeta)
)]
public partial class User
{
    // Properties...
}
```

### Attribute Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `CollectionName` | `string` | No | The MongoDB collection name. Defaults to the class name. |
| `DatabaseName` | `string` | No | The MongoDB database name. Can be overridden at runtime. |
| `MetadataType` | `Type` | No | A custom metadata type for the document. |

---

## Partial Properties

MongoObject uses C# 14 partial properties to enable automatic change tracking:

```csharp
public partial class User
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }
    public partial int Age { get; set; }
}
```

### Requirements

1. **Class must be partial**: The containing class must be declared as `partial`
2. **Properties must be partial**: Each tracked property must use the `partial` keyword
3. **Public setters required**: Properties must have public getters and setters

### Supported Types

MongoObject supports a wide range of property types:

| Category | Types |
|----------|-------|
| **Primitives** | `int`, `long`, `double`, `decimal`, `bool`, `DateTime`, `Guid`, `string` |
| **Collections** | `List<T>`, `ObservableCollection<T>`, `IEnumerable<T>` |
| **Dictionaries** | `Dictionary<TKey, TValue>` |
| **Nested Objects** | Any class (tracked recursively if it's a `TrackingObservableObject`) |
| **Nullable Types** | All nullable value types (`int?`, `DateTime?`, etc.) |

---

## Complete Document Example

```csharp
using MongoDB.Bson.Serialization.Attributes;
using MongoObject.Core.Attributes;
using System.Collections.ObjectModel;

namespace MyApp.Documents;

// Optional: Define a metadata type
public partial record UserMeta
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// Define the document
[MongoObject(
    CollectionName = "Users",
    DatabaseName = "MyApp",
    MetadataType = typeof(UserMeta)
)]
[BsonIgnoreExtraElements]  // Recommended for forward compatibility
public partial class User
{
    // Simple properties
    public partial string Name { get; set; }
    public partial string Email { get; set; }
    public partial int Age { get; set; }
    
    // Nullable property
    public partial DateTime? Birthday { get; set; }
    
    // Collection property
    public partial ObservableCollection<string> Roles { get; set; }
    
    // Dictionary property
    public partial Dictionary<string, string> Preferences { get; set; }
    
    // Nested object (not tracked for changes unless it inherits TrackingObservableObject)
    public partial Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}
```

NOTE: You can decorate the `Address` class as a `[MongoObject]` this will add the `TrackingObservableObject` but it will also allow you to save it seperately if you so choose. Currently this is the recommended path.

---

## Property Name Changes

Use `[PropertyNameChange]` to handle property renames while maintaining backward compatibility:

```csharp
public partial class User
{
    [PropertyNameChange("UserName")]  // Old name in MongoDB
    public partial string Name { get; set; }
}
```

This allows MongoObject to read documents with the old property name while writing with the new name.

NOTE: This is still in developement and isn't in a working state yet

---

## BSON Attributes

MongoObject works with standard MongoDB.Driver BSON attributes:

```csharp
[BsonIgnoreExtraElements]  // Ignore unknown fields when deserializing
public partial class User
{
    [BsonElement("full_name")]  // Custom field name in MongoDB
    public partial string Name { get; set; }
    
    [BsonIgnore]  // Don't persist this property
    public string ComputedField => $"User: {Name}";
}
```

---

## What Gets Generated

When you build your project, the source generator creates:

### 1. Document Implementation

```csharp
public partial class User : TrackingObservableObject, IDocumentFile, IDisposable
{
    public partial string Name
    {
        get => field;
        set => SetField(ref field, value);
    }
    
    // ... other properties
    
    public override void TrackChanges(TrackingObservableObject observable, bool isTracking, string parentName)
    {
        // Generated change tracking implementation
    }
}
```

### 2. Search Class

```csharp
public class UserSearch : IClassSearch<User>
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
    // ... other properties
}
```

### 3. Metadata Types (if MetadataType specified)

```csharp
public class UserMetaQuery : IMetadataSearchBase
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
}

public class UserMetaRecord : IMetadataBase
{
    public string? CreatedBy { get; set; }
    public string? Department { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

---

## Best Practices

### 1. Use Records for Metadata Types

```csharp
// ✓ Recommended
public partial record UserMeta
{
    public string? CreatedBy { get; set; }
}

// Less ideal
public class UserMeta
{
    public string? CreatedBy { get; set; }
}
```

### 2. Apply BsonIgnoreExtraElements

This attribute prevents deserialization errors when documents have fields that don't exist in your class:

```csharp
[BsonIgnoreExtraElements]
public partial class User { }
```

### 3. Initialize Collection Properties

```csharp
public partial class User
{
    public partial List<string> Roles { get; set; } = new();
}
```

### 4. Keep Metadata Types Simple

Metadata types should contain simple, serializable properties:

```csharp
public partial record DocumentMeta
{
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? OwnerId { get; set; }
}
```

---

## Next Steps

- **[Change Tracking](change-tracking.md)** - Learn how changes are detected and persisted
- **[Metadata](metadata.md)** - Deep dive into metadata types and versioning
- **[Searching](searching.md)** - Query documents using generated search classes