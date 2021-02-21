using GraphQLTest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.POCO
{
    public interface IGraphQLResolver
    {
        Task<IEnumerable<object>> GetAllAsync(EntityMetadataContext metadata);
        Task<object> GetByKeyAsync(EntityMetadataContext metadata, params KeyValuePair<EntityMetadataProp, object>[] key);
    }
}
