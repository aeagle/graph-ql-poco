using GraphQL.Language.AST;
using GraphQLTest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.POCO
{
    public interface IGraphQLResolver
    {
        Task<IEnumerable<dynamic>> GetAsync(
            IHaveSelectionSet context, 
            EntityMetadataContext metadata);
    }
}
