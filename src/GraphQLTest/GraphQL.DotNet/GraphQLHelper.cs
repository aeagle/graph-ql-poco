using Microsoft.Extensions.DependencyInjection;
using System;

namespace GraphQLTest
{
    public static class GraphQLHelper
    {
        public static IServiceCollection SetupGraphQLSchema(
            this IServiceCollection services,
            Action<GraphQLSchema> configure)
        {
            var schema = new GraphQLSchema();

            configure?.Invoke(schema);

            services.AddSingleton(schema.Build());

            return services;
        }
    }
}
