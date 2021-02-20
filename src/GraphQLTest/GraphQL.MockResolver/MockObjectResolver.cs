using GraphQL.POCO;
using GraphQLTest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.MockResolver
{
    public class MockObjectResolver : IGraphQLResolver
    {
        public IEnumerable<object> GetAll(EntityMetadataContext metadata)
        {
            return Enumerable.Range(1, 10).Select(e =>
            {
                var instance = Activator.CreateInstance(metadata.Type);
                return instance;
            });
        }

        public object GetByKey(params KeyValuePair<EntityMetadataProp, object>[] key)
        {
            throw new NotImplementedException();
        }
    }
}
