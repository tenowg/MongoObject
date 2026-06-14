using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IMetadataSearchBase
    {
        public QueryVal<DateTime>? CreatedAt { get; set; }
        public QueryVal<DateTime>? LastModifiedAt { get; set; }
        public QueryVal<long>? Version { get; set; }
        public FilterDefinition<MongoDocument<T>> ToMongoFilter<T>() where T : class, IDocumentFile, new();
    }

    public abstract record MetadataSearch
    {
        public static FilterDefinition<TDocument> CreateFilter<TDocument, TValue>(
        FilterDefinitionBuilder<TDocument> builder,
        string fieldPath,
        QueryVal<TValue> queryVal)
        {
            return queryVal switch
            {
                QueryVal<TValue>.EqualTo eq => builder.Eq(fieldPath, eq.Value),
                QueryVal<TValue>.NotEquals ne => builder.Ne(fieldPath, ne.Value),
                QueryVal<TValue>.GreaterThan gt => builder.Gt(fieldPath, gt.Value),
                QueryVal<TValue>.GreaterThanOrEqual gte => builder.Gte(fieldPath, gte.Value),
                QueryVal<TValue>.LessThan lt => builder.Lt(fieldPath, lt.Value),
                QueryVal<TValue>.LessThanOrEqual lte => builder.Lte(fieldPath, lte.Value),
                QueryVal<TValue>.IsNull => builder.Eq(fieldPath, BsonNull.Value),
                QueryVal<TValue>.IsNotNull => builder.Ne(fieldPath, BsonNull.Value),
                QueryVal<TValue>.All all => builder.All(fieldPath, all.Values),
                QueryVal<TValue>.In @in => builder.In(fieldPath, @in.Values),
                QueryVal<TValue>.Or or => builder.Or(or.Conditions.Select(c => CreateFilter(builder, fieldPath, c))),
                QueryVal<TValue>.Like like => builder.Regex(fieldPath, new BsonRegularExpression(like.Pattern, like.Options)),
                QueryVal<TValue>.Range range => builder.And(
                    builder.Gte(fieldPath, range.Min),
                    builder.Lte(fieldPath, range.Max)
                ),
                QueryVal<TValue>.And and => builder.And(and.Conditions.Select(c => CreateFilter(builder, fieldPath, c))),
                _ => throw new NotSupportedException($"Query type {queryVal.GetType().Name} is not supported.")
            };
        }
    }

    public interface IMetadataBase
    {
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public long? Version { get; set; }
    }
}
