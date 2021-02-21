using GraphQL.Language.AST;
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
        public Task<IEnumerable<object>> GetAsync(
            IHaveSelectionSet context, 
            EntityMetadataContext metadata)
        {
            return Task.FromResult(Enumerable.Range(1, 10).Select(e =>
            {
                var instance = Activator.CreateInstance(metadata.Type);
                return instance;
            }));
        }
    }
}
