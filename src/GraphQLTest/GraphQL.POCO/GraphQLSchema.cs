using GraphQL.POCO;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQLTest
{
    public class GraphQLSchema
    {
        private readonly IDictionary<Type, EntityMetadataContext> metadata =
            new Dictionary<Type, EntityMetadataContext>();

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

        public GraphQLSchema Add<T>(Action<EntityMetadataContext<T>> config)
        {
            if (!metadata.TryGetValue(typeof(T), out var entityMetadata))
            {
                entityMetadata = EntityMetadata.Bind<T>().BindAllProperties();
                metadata.Add(typeof(T), entityMetadata);
            }

            config((EntityMetadataContext<T>)entityMetadata);

            return this;
        }

        public ISchema Build()
        {
            var root = new ObjectGraphType
            {
                Name = "QueryRoot"
            };

            foreach (var entityMetadata in metadata.Values)
            {
                var entity = new ObjectGraphType();
                entity.Name = GraphQLName(entityMetadata.Type.Name);

                foreach (var prop in entityMetadata.Properties)
                {
                    entity.Field(GraphQLType(prop.Value.Info.PropertyType), GraphQLName(prop.Key));
                }

                var collection = new ListGraphType(entity);

                root.Field(
                    PluralGraphQLName(entity.Name),
                    collection,
                    resolve: context => defaultResolver.GetAll(entityMetadata)
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
