# Getting Started

This guide will help you set up MongoObject in your .NET 10 project and create your first MongoDB document.

---

## Prerequisites

Before you begin, ensure you have:

- **.NET 10 SDK** installed
- **MongoDB 4.0+** running (local or remote)
- An IDE with C# 14 support (Visual Studio 2022 17.x+ or VS Code with C# Dev Kit)

---

## Installation

> **📦 NuGet Package: Coming Soon**
>
> The `Tenowg.MongoObjects` package is currently in active development and will be published to NuGet once it reaches stability.

### Using Project Reference (Current Method)

1. Clone the MongoObject repository:

```bash
git clone https://github.com/tenowg/MongoObjects.git
```

2. Add a project reference to your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/MongoObject.Core/MongoObject.Core.csproj" />
</ItemGroup>
```

---

## Your First Document

### Step 1: Define a Document Class

Create a new partial class and decorate it with the `[MongoObject]` attribute:

[!code-csharp[Product](../../Examples/ConsoleSetup/Models/Product.cs#Product)]

**Key points:**
- The class must be `partial` for the source generator to work
- Properties you want tracked must also be `partial`
- The `[MongoObject]` attribute specifies the collection and database names

### Step 2: Register in Dependency Injection

In your `Program.cs` or startup file:

[!code-csharp[RegisterMongoOption](../../Examples/ConsoleSetup/Program.cs#ConfigureMongoObject)]

### Step 3: Use the Document Monitor

Inject `IDocumentMonitor<T>` into your services:

```csharp
public class ProductService
{
    private readonly IDocumentMonitor<Product> _monitor;

    public ProductService(IDocumentMonitor<Product> monitor)
    {
        _monitor = monitor;
    }

    // Create a new product
    public async Task<string> CreateProductAsync(Product product)
    {
        return await _monitor.Add(product);
    }

    // Get a product by ID
    public async Task<Product?> GetProductAsync(string id)
    {
        return await _monitor.Get(id);
    }

    // Update a product
    public async Task UpdateProductPriceAsync(string id, decimal newPrice)
    {
        // using Get is similar to EF's Attach, becuase all documents are cached
        // in a per App cache, this will either immediately return a ref to your document
        // or will fetch it from the database. This will ensure that the document you are
        // updating is always fresh.
        var product = await _monitor.Get(id);
        product.Price = newPrice;  // Change is automatically tracked
        await _monitor.SaveChanges(product);
    }
}
```

---

## Understanding the Generated Code

When you build your project, the source generator automatically creates:

### 1. Partial Class Implementation

```csharp
// Auto-generated
public partial class Product : TrackingObservableObject, IDocumentFile
{
    public partial string Name
    {
        get => field;
        set => SetField(ref field, value);
    }
    // ... other properties
}
```

### 2. Search Classes

```csharp
// Auto-generated
public class ProductSearch : IClassSearch<Product>
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    // ... other properties
}
```

---

## Next Steps

Now that you have a basic document set up, explore these topics:

- **[Defining Documents](defining-documents.md)** - Learn about all the options for the `[MongoObject]` attribute
- **[Change Tracking](change-tracking.md)** - Understand how MongoObject tracks changes
- **[Metadata](metadata.md)** - Add versioning and timestamps to your documents
- **[Searching](searching.md)** - Query documents with type-safe search classes

---

## Common Issues

### "Partial property requires the containing type to be partial"

Ensure your class is declared as `partial`:

```csharp
public partial class Product  // ✓ Correct
public class Product          // ✗ Missing partial keyword
```

### "No service for type IDocumentMonitor<T>"

Make sure you've registered your documents:

```csharp
services.AddMongoObject(options => { ... })
        .RegisterDocumentsFromAssembly();  // Don't forget this!
```

### Change streams not working

For local MongoDB development, add `directConnection=true` to your connection string:

```csharp
options.ConnectionString = "mongodb://localhost:27017/?directConnection=true";