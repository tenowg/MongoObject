# Product Context

## Why MongoObject Exists

MongoObject was created to address the gap between MongoDB's document model and traditional .NET development patterns. While MongoDB.Driver provides excellent low-level access, developers often need a higher-level abstraction that:

1. **Reduces Boilerplate**: Eliminates repetitive code for document mapping and change tracking
2. **Provides Type Safety**: Offers compile-time validation of document structures
3. **Simplifies Updates**: Automatically tracks changes and generates efficient update operations
4. **Integrates with .NET Ecosystem**: Uses familiar patterns like dependency injection and attributes

## Problems It Solves

### 1. Change Tracking Complexity
**Problem**: Manually tracking which fields changed in a document is error-prone and tedious.
**Solution**: Source generator creates partial classes that automatically track property changes via `INotifyPropertyChanged`.

### 2. Metadata Management
**Problem**: Documents often need associated metadata (version, timestamps, ownership) that's separate from business data.
**Solution**: Built-in metadata support with separate query and record types generated automatically.

### 3. Update Efficiency
**Problem**: Updating entire documents when only a few fields changed is wasteful.
**Solution**: Change tracking generates precise `$set` and `$unset` operations for only modified fields.

### 4. Type-Safe Queries
**Problem**: Building MongoDB filters with string-based field names is error-prone.
**Solution**: Generated search classes provide type-safe query building.

## How It Should Work

### Developer Experience
```csharp
// 1. Define a document with attributes
[MongoObject(CollectionName = "Users", DatabaseName = "MyApp", MetadataType = typeof(UserMeta))]
public partial class User
{
    public partial string Name { get; set; }
    public partial int Age { get; set; }
    public partial Address Address { get; set; }
}

// 2. Register in DI
services.AddMongoObject(options => { ... })
        .RegisterDocumentsFromAssembly();

// 3. Use via IDocumentMonitor<T>
var user = await monitor.Get(userId);
user.Name = "New Name";  // Automatically tracked
await monitor.SaveChanges(user);  // Only sends changed fields
```

### Key Design Principles
1. **Convention over Configuration**: Sensible defaults with override options
2. **Non-Invasive**: Uses partial classes and attributes, doesn't require inheritance
3. **Performance-First**: Caching, efficient updates, and minimal allocations
4. **Type-Safe**: Compile-time validation via source generators

## User Experience Goals

1. **Quick Onboarding**: Developers should be productive within minutes
2. **Familiar Patterns**: Uses .NET conventions (attributes, DI, async/await)
3. **Transparent**: Clear what's happening under the hood
4. **Flexible**: Supports both simple and complex scenarios
5. **Observable**: Built-in change notification for reactive patterns