using MongoDB.Bson;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using static MongoDB.Driver.WriteConcern;

namespace MongoObject.Core.Data
{
    public record SortField<T>(Expression<Func<T, object>> Selector, bool Descending = false);
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
        public record In(T[] Values) : QueryVal<T>;
        public record Or(params QueryVal<T>[] Conditions) : QueryVal<T>;
        public record Range(T Min, T Max) : QueryVal<T>;
        public record And(params QueryVal<T>[] Conditions) : QueryVal<T>;
        public record IsNull() : QueryVal<T>;
        public record IsNotNull() : QueryVal<T>;
        public record Like(string Pattern, string Options) : QueryVal<T>;
        public record All(T[] Values) : QueryVal<T>;
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

            public static QueryVal<T> operator >(QueryVal<T>? left, T right) => new QueryVal<T>.GreaterThan(right);
            public static QueryVal<T> operator <(QueryVal<T>? left, T right) => new QueryVal<T>.LessThan(right);
            public static QueryVal<T> operator >=(QueryVal<T>? left, T right) => new QueryVal<T>.GreaterThanOrEqual(right);
            public static QueryVal<T> operator <=(QueryVal<T>? left, T right) => new QueryVal<T>.LessThanOrEqual(right);

            public QueryVal<T> Or(params QueryVal<T>[] values) => new QueryVal<T>.Or(values);
            public QueryVal<T> In(params T[] values) => new QueryVal<T>.In(values);
            public QueryVal<T> Range(T min, T max) => new QueryVal<T>.Range(min, max);
            public QueryVal<T> Range((T Min, T Max) range) => new QueryVal<T>.Range(range.Min, range.Max);
            public QueryVal<T> InverseRange(T min, T max) => new QueryVal<T>.Or(new QueryVal<T>.LessThan(min), new QueryVal<T>.GreaterThan(max));
            public QueryVal<T> And(params QueryVal<T>[] values)=> new QueryVal<T>.And(values);
        }

        extension(QueryVal<DateTime>? value)
        {
            public QueryVal<DateTime> InLast24Hours => new QueryVal<DateTime>.GreaterThanOrEqual(DateTime.UtcNow.AddHours(-24));
        }

        extension(QueryVal<string>? q)
        {
            public QueryVal<string> Contains(string value, bool ignoreCase = true) => new QueryVal<string>.Like($".*{Regex.Escape(value)}.*", ignoreCase ? "i" : "");
            public QueryVal<string> StartsWith(string value, bool ignoreCase = true) => new QueryVal<string>.Like($"^{Regex.Escape(value)}", ignoreCase ? "i" : "");
        }
    }
}