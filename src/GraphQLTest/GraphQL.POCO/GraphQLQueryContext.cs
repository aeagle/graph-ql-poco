using System;
using System.Linq.Expressions;

namespace GraphQL.POCO
{
    public abstract class GraphQLQueryContext
    {
    }

    public class GraphQLQueryContext<T> : GraphQLQueryContext
    {
        public GraphQLQueryContext<T> QueryableBy(params Expression<Func<T, object>>[] fields)
        {
            return this;
        }
    }
}
