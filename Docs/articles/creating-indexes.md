---
uid: cli-docs
---

# Creating Indexes

Indexes in MongoObject are defined by applying attributes to your model class and its properties. This page describes the attributes required to configure indexes; index source generation and related implementation details are covered separately.

## Define an Index

Apply the `[MongoIndex]` attribute to the model class for each index that should be created.

Multiple `[MongoIndex]` attributes can be applied to the same class when the collection requires multiple indexes.

### MongoIndex Attribute

The `[MongoIndex]` attribute supports the following values:

| Value | Required | Description |
|---|---:|---|
| Index name | Yes | The name used to identify the index. Provided as the constructor argument. |
| `Unique` | No | Specifies whether the index enforces unique values. Defaults to `false`. |

## Add Fields to an Index

Apply the `[FieldIndex]` attribute to each property that should participate in an index.

### FieldIndex Attribute

The `[FieldIndex]` attribute supports the following values:

| Value | Required | Description |
|---|---:|---|
| Index name | Yes | The name of the `[MongoIndex]` definition to which the property belongs. |
| `Type` | No | The MongoObject index type to use. Defaults to `IndexType.Index`. |
| `Direction` | No | The sort direction for the field. Defaults to `IndexDirection.Ascending`. Supported values are `Ascending` and `Descending`. |

## Example

The following example defines a unique ascending index on the `Name` property:

```csharp
[MongoObject]
[MongoIndex("IndexName", Unique = true)]
public partial class Product
{
    [FieldIndex(
        "IndexName",
        Type = IndexType.Index,
        Direction = IndexDirection.Ascending)]
    public string Name { get; set; }
}
```

## Apply the Index Migration

After defining your indexes, build a migration:

```bash
mo migrate build -p ../PathtoProject
```

Review the generated migration. If it is correct, apply it to the database:

```bash
mo migrate run -p ../PathtoProject
```

## Additional Resources

- **[CLI Documentation](xref:cli-docs)** — Learn about MongoObject CLI commands.