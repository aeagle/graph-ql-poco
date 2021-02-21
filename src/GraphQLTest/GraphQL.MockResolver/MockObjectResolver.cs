using GraphQL.POCO;
using GraphQLTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.MockResolver
{
    public class MockObjectResolver : IGraphQLResolver
    {
        public Task<IEnumerable<object>> GetAllAsync(EntityMetadataContext metadata)
        {
            return Task.FromResult(Enumerable.Range(1, 10).Select(e =>
            {
                var instance = Activator.CreateInstance(metadata.Type);
                return instance;
            }));
        }

        public Task<object> GetByKeyAsync(
            EntityMetadataContext metadata, 
            params KeyValuePair<EntityMetadataProp, object>[] key)
        {
            return Task.FromResult(Activator.CreateInstance(metadata.Type));
        }
    }
}
