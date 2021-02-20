using GraphQL.POCO;
using GraphQLTest;
using System;
using System.Collections.Generic;
using System.Data;

namespace GraphQL.SQLResolver
{
    public class SQLResolver : IGraphQLResolver
    {
        private readonly Func<IDbConnection> connectionFactory;

        public SQLResolver(Func<IDbConnection> connectionFactory)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public IEnumerable<object> GetAll(EntityMetadataContext metadata)
        {
            throw new NotImplementedException();
        }

        public object GetByKey(params KeyValuePair<EntityMetadataProp, object>[] key)
        {
            throw new NotImplementedException();
        }
    }
}
