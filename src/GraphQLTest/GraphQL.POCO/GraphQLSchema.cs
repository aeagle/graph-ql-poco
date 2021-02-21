using GraphQL.POCO;
using GraphQL.Resolvers;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQLTest
{
    public class GraphQLSchema
    {
        private readonly IDictionary<Type, GraphQLEntityContext> metadata =
            new Dictionary<Type, GraphQLEntityContext>();

        private IGraphQLResolver defaultResolver = null;

        private static string GraphQLName(string name)
        {
            return name.ToLower();
        }

        private static string PluralGraphQLName(string name)
        {
            return $"{GraphQLName(name)}s";
        }

        private static Type GraphQLType(Type type)
        {
            if (type == typeof(string))
            {
                return typeof(StringGraphType);
            }
            if (type == typeof(Guid) || type == typeof(Guid?))
            {
                return typeof(StringGraphType);
            }
            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return typeof(StringGraphType);
            }
            if (type == typeof(int) || type == typeof(int?))
            {
                return typeof(IntGraphType);
            }

            return typeof(StringGraphType);
        }

        public GraphQLSchema DefaultResolver(IGraphQLResolver resolver)
        {
            defaultResolver = resolver;

            return this;
        }

        public GraphQLSchema Add<T>(Action<GraphQLEntityContext<T>> configure)
        {
            if (!metadata.TryGetValue(typeof(T), out var graphQLEntity))
            {
                graphQLEntity = new GraphQLEntityContext<T>(EntityMetadata.Bind<T>().BindAllProperties());
                metadata.Add(typeof(T), graphQLEntity);
            }

            configure((GraphQLEntityContext<T>)graphQLEntity);

            return this;
        }

        public ISchema Build()
        {
            var root = new ObjectGraphType
            {
                Name = "QueryRoot"
            };

            foreach (var graphQLEntity in metadata.Values)
            {
                var entity = new ObjectGraphType();
                entity.Name = GraphQLName(graphQLEntity.Entity.Type.Name);

                foreach (var prop in graphQLEntity.Entity.Properties)
                {
                    entity.FieldAsync(
                        GraphQLType(prop.Value.Info.PropertyType), 
                        GraphQLName(prop.Key),
                        resolve: async context =>
                        {
                            if (context != null && context.Source is IDictionary<string, object> dynamicObject)
                            {
                                var name = context?.FieldAst?.Name;
                                if (name != null)
                                {
                                    if (dynamicObject.TryGetValue(name, out var val))
                                    {
                                        return await Task.FromResult(val);
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException($"Expected to find property or method '{name}' on type '{GraphQLName(graphQLEntity.Entity.Type.Name)}' but it does not exist.");
                                    }
                                }
                            }
                            return await NameFieldResolver.Instance.ResolveAsync(context);
                        }
                    );
                }

                var collection = new ListGraphType(entity);

                root.FieldAsync(
                    PluralGraphQLName(entity.Name),
                    collection,
                    resolve: async context => await defaultResolver.GetAllAsync(graphQLEntity.Entity)
                );
            }

            var schemaDynamic = new Schema
            {
                Query = root
            };

            return schemaDynamic;
        }
    }
}
