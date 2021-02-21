using GraphQL.POCO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQLTest
{
    public class EntityMetadata
    {
        private static IDictionary<Type, EntityMetadataContext> metadata = 
            new Dictionary<Type, EntityMetadataContext>();

        public static EntityMetadataContext<T> Get<T>()
        {
            return (EntityMetadataContext<T>)Get(typeof(T));
        }

        public static EntityMetadataContext Get(Type type)
        {
            if (!metadata.TryGetValue(type, out var entityMetadata))
            {
                entityMetadata =
                    (EntityMetadataContext)Activator.CreateInstance(
                        typeof(EntityMetadataContext<>).MakeGenericType(type)
                    );
                metadata.Add(type, entityMetadata);
            }

            return entityMetadata;
        }
    }

    public abstract class EntityMetadataContext
    {
        public Type Type { get; protected set; }

        public Func<object, object> PrimaryKey { get; protected set; }

        public Dictionary<string, EntityMetadataProp> Properties { get; private set; } =
            new Dictionary<string, EntityMetadataProp>();

        public Dictionary<string, EntityMetadataProp> Included { get; private set; } =
            new Dictionary<string, EntityMetadataProp>();

        public Dictionary<string, EntityMetadataProp> Keys { get; private set; } =
            new Dictionary<string, EntityMetadataProp>();

        public Dictionary<string, EntityMetadataRelation> Relations { get; private set; } =
            new Dictionary<string, EntityMetadataRelation>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> CustomMetadata { get; private set; } =
            new Dictionary<string, object>();
    }

    public class EntityMetadataContext<T> : EntityMetadataContext
    {
        public EntityMetadataContext()
        {
            Type = typeof(T);

            var props = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var prop in props)
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    var collectionType = prop.PropertyType.GetGenericArguments()[0];
                    Relations.Add(
                        prop.Name,
                        new EntityMetadataRelation()
                        {
                            Name = prop.Name,
                            Info = prop,
                            EntityLeft = this,
                            IsCollection = true,
                            EntityRightType = collectionType,
                        }
                    );
                }
                else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    Relations.Add(
                        prop.Name,
                        new EntityMetadataRelation()
                        {
                            Name = prop.Name,
                            Info = prop,
                            EntityLeft = this,
                            IsCollection = false,
                            EntityRightType = prop.PropertyType
                        }
                    );
                }
                else
                {
                    Properties.Add(
                        prop.Name,
                        new EntityMetadataProp()
                        {
                            Name = prop.Name,
                            Info = prop
                        }
                    );
                }
            }
        }

        public EntityMetadataContext<T> BindAllProperties()
        {
            Included.Clear();
            foreach (var prop in Properties)
            {
                Included.Add(prop.Key, prop.Value);
            }

            return this;
        }

        public EntityMetadataContext<T> Key(Expression<Func<T, object>> keyExpression)
        {
            var runnable = keyExpression.Compile();
            PrimaryKey = (instance) => runnable((T)instance);

            if (Properties.TryGetValue(ExpressionHelper.GetPropertyName(keyExpression), out var prop))
            {
                Keys.Add(prop.Name, prop);
            }
            else
            {
                throw new InvalidOperationException("Invalid expression");
            }

            return this;
        }

        public EntityMetadataContext<T> Ignore(params Expression<Func<T, object>>[] fields)
        {
            foreach (var field in fields)
            {
                Included.Remove(ExpressionHelper.GetPropertyName(field));
            }

            return this;
        }
    }

    public class EntityMetadataProp
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PropertyInfo Info { get; set; }
    }

    public class EntityMetadataRelation
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PropertyInfo Info { get; set; }
        public EntityMetadataContext EntityLeft { get; set; }
        public bool IsCollection { get; set; }
        public Type EntityRightType { get; set; }

        public void Set(dynamic instance, dynamic value)
        {
            Info.SetValue(instance, value);
        }

        public void Add(dynamic instance, dynamic value)
        {
            var list = Info.GetValue(instance);
            list.Add(value);
        }

        public EntityMetadataContext EntityRight => 
            EntityMetadata.Get(EntityRightType);

        public IDictionary<string, string> EntityRightForeignKeys =>
            IsCollection ?
                EntityLeft.Keys.Values
                    .Select(k =>
                        new
                        {
                            key = k.Name,
                            foreign = $"{EntityLeft.Type.Name}{k.Name}"
                        }
                    )
                    .ToDictionary(x => x.key, x => x.foreign)
                :
                EntityRight.Keys.Values
                    .Select(k =>
                        new
                        {
                            key = $"{EntityRight.Type.Name}{k.Name}",
                            foreign = $"{k.Name}"
                        }
                    )
                    .ToDictionary(x => x.key, x => x.foreign);
    }
}
