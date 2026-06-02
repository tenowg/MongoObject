namespace MongoObject.Core.Data
{
    public abstract record QueryVal<T>
    {
        // The implicit operator allows 'Age: 32' to automatically become 'new EqualTo(32)'
        public static implicit operator QueryVal<T>(T value) => new EqualTo(value);

        // The nested types inherit from QueryVal<T>
        public record EqualTo(T Value) : QueryVal<T>;
        public record LessThan(T Value) : QueryVal<T>;
        public record LessThanOrEqual(T Value) : QueryVal<T>;
        public record GreaterThan(T Value) : QueryVal<T>;
        public record GreaterThanOrEqual(T Value) : QueryVal<T>;
        public record NotEquals(T Value) : QueryVal<T>;
    }

    public static class QueryExtensions
    {
        extension<T>(QueryVal<T>? q)
        {
            public QueryVal<T> Lt(T right) => new QueryVal<T>.LessThan(right);
            public QueryVal<T> Gt(T right) => new QueryVal<T>.GreaterThan(right);
            public QueryVal<T> Lte(T right) => new QueryVal<T>.LessThanOrEqual(right);
            public QueryVal<T> Gte(T right) => new QueryVal<T>.GreaterThanOrEqual(right);
            public QueryVal<T> Ne(T right) => new QueryVal<T>.NotEquals(right);

            public static QueryVal<T> operator >(QueryVal<T>? left, T right)
                => new QueryVal<T>.GreaterThan(right);

            public static QueryVal<T> operator <(QueryVal<T>? left, T right)
                => new QueryVal<T>.LessThan(right);

            public static QueryVal<T> operator >=(QueryVal<T>? left, T right)
                => new QueryVal<T>.GreaterThanOrEqual(right);

            public static QueryVal<T> operator <=(QueryVal<T>? left, T right)
                => new QueryVal<T>.LessThanOrEqual(right);
        }

        extension(QueryVal<DateTime>? value)
        {
            public QueryVal<DateTime> InLast24Hours
                => new QueryVal<DateTime>.GreaterThanOrEqual(DateTime.UtcNow.AddHours(-24));
        }
    }
}