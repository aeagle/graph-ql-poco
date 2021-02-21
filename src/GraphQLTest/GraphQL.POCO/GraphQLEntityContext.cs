using GraphQLTest;
using System;

namespace GraphQL.POCO
{
    public abstract class GraphQLEntityContext
    {
        protected EntityMetadataContext baseEntity { get; set; }
        protected GraphQLQueryContext baseQuery { get; set; }

        public GraphQLQueryContext Query => baseQuery;
        public EntityMetadataContext Entity => baseEntity;
    }

    public class GraphQLEntityContext<T> : GraphQLEntityContext
    {
        public GraphQLEntityContext(EntityMetadataContext entityMetadata)
        {
            baseEntity = entityMetadata;
            baseQuery = new GraphQLQueryContext<T>();
        }

        public GraphQLEntityContext<T> EntityConfig(Action<EntityMetadataContext<T>> configure)
        {
            configure((EntityMetadataContext<T>)baseEntity);
            return this;
        }

        public GraphQLEntityContext<T> QueryConfig(Action<GraphQLQueryContext<T>> configure)
        {
            configure((GraphQLQueryContext<T>)baseQuery);
            return this;
        }
    }
}
