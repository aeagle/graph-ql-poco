using GraphQLTest;
using System.Collections.Generic;

namespace GraphQL.POCO
{
    public interface IGraphQLResolver
    {
        IEnumerable<object> GetAll(EntityMetadataContext metadata);
        object GetByKey(params KeyValuePair<EntityMetadataProp, object>[] key);
    }
}
