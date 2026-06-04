# Fluent Search
While MongoObject also provides code generated QueryBuilders. This allows for full fluent query building for most operations.

## Basic Search

``` csharp
IEnumerable results = await monitor.Search()
    .WithQuery(f => {
        f.Name = "John",
        f.Age = 28
        });
```

This would be `Look for John who is 28 years old`, a simple AND query.
If we have several Johns who you want to filter for range, there are a few
different way to build this query. You can use the Range option, you can even build a And query

``` csharp
IEnumerable results = await monitor.Search()
    .WithQuery(f => {
        f.Name = "John",
        f.Age = f.Age.Range(15, 20)
    });

OR (A much more verbose method)

IEnumerable results = await monitor.Search()
    .WithQuery(f => {
        f.Name = "John",
        f.Age = f.Age.And(
            f.Age = f.Age.Lt(20),
            f.Age = f.Age.Gt(15)
        );
    });
```

These two queries are the same under the hood, they both look for any John that is between the ages of 15 and 20.

[!code-csharp[ExampleQuery](../../Progress/App.cs#QueryExample)]