using GraphQL;
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
                graphQLEntity = new GraphQLEntityContext<T>(EntityMetadata.Get<T>().BindAllProperties());
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

            Dictionary<Type, ObjectGraphType> objectTypes = new Dictionary<Type, ObjectGraphType>();

            async Task<object> resolveField(IResolveFieldContext<object> context, Type type)
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
                            throw new InvalidOperationException($"Expected to find property or method '{name}' on type '{GraphQLName(type.Name)}' but it does not exist.");
                        }
                    }
                }
                return await NameFieldResolver.Instance.ResolveAsync(context);
            }

            foreach (var graphQLEntity in metadata.Values)
            {
                var entity = new ObjectGraphType();
                entity.Name = graphQLEntity.Entity.Type.Name;

                foreach (var prop in graphQLEntity.Entity.Included)
                {
                    entity.FieldAsync(
                        GraphQLType(prop.Value.Info.PropertyType),
                        GraphQLName(prop.Key),
                        resolve: (ctx) => resolveField(ctx, graphQLEntity.Entity.Type)
                    );
                }

                objectTypes.Add(graphQLEntity.Entity.Type, entity);
            }

            foreach (var graphQLEntity in metadata.Values)
            {
                var entity = objectTypes[graphQLEntity.Entity.Type];

                foreach (var relation in graphQLEntity.Entity.Relations)
                {
                    if (relation.Value.IsCollection)
                    {
                        entity.FieldAsync(
                            GraphQLName(relation.Key),
                            new ListGraphType(objectTypes[relation.Value.EntityRightType])
                        );
                    }
                    else
                    {
                        entity.FieldAsync(
                            GraphQLName(relation.Key),
                            objectTypes[relation.Value.EntityRightType]
                        );
                    }
                }

                root.FieldAsync(
                    PluralGraphQLName(entity.Name),
                    new ListGraphType(entity),
                    resolve: async context => await defaultResolver.GetAsync(context.FieldAst, graphQLEntity.Entity)
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
