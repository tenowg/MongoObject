# Vector Searches and Indexes
MongoObject supports defining Vector Indexes and Vector Search out of the box.

> [!NOTE]
> MongoObject will check the capabilities of your server to determine if you
> can run Vector Search. To use Vector Search your Database needs to be a replica set.
> And either Enterprise or Atlas server.

You set up Vectors just like Projections, it uses the same structure.

``` csharp
[MongoObject]
public partial class User
{
    [ProjectValue("Description", ProjectionType.AutoVector, Similarity = VectorSimilarity.Cosine)]
    public partial string Description { get; set; }
```