using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IMetadataSearchBase
    {
        public QueryVal<DateTime>? CreatedAt { get; set; }
        public QueryVal<DateTime>? LastModifiedAt { get; set; }
        public FilterDefinition<MongoDocument<T>> ToMongoFilter<T>() where T : class, IDocumentFile, new();
        //public void FromDictionary(Dictionary<string, object> dictionary);
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
                _ => throw new NotSupportedException($"Query type {queryVal.GetType().Name} is not supported.")
            };
        }
    }

    public interface IMetadataBase
    {
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public int? Version { get; set; }
    }
}
