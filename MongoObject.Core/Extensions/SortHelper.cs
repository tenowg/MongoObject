using MongoDB.Driver;
using MongoObject.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MongoObject.Core.Extensions
{
    public static class SortHelper
    {
        public static string GetFieldName<T>(Expression<Func<T, object>> expression)
        {
            var parts = new Stack<string>();
            Expression current = expression.Body;

            // Unwrap UnaryExpression (boxing of value types)
            if (current is UnaryExpression unary)
                current = unary.Operand;

            
            while (current is MemberExpression member)
            {
                parts.Push(member.Member.Name);
                current = member.Expression;
            }
            parts.Push("Document");
            return string.Join(".", parts);
        }

        public static SortDefinition<T> BuildSortDefinition<T, TSort>(params SortField<TSort>[] fields)
        {
            var builder = Builders<T>.Sort;
            var sortDefs = fields.Select(f =>
                f.Descending
                    ? builder.Descending(GetFieldName(f.Selector))
                    : builder.Ascending(GetFieldName(f.Selector))
            );
            return builder.Combine(sortDefs);
        }
    }
}
