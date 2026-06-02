``` csharp
public record UserMetadata(
    QueryVal<string>? OwnerId = null, 
    QueryVal<int>? Age = null
);
```

``` csharp
// 1. Exact match (Triggers the implicit operator -> turns into QueryVal.Equals)
var exactQuery = new UserMetadata(OwnerId: "user_123", Age: 30);

// 2. Complex match (Using your nested classes)
var complexQuery = new UserMetadata(
    OwnerId: "user_123", 
    Age: new QueryVal<int>.LessThan(30)
);
```