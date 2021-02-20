using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQLTest
{
    public class EntityMetadata
    {
        private static IDictionary<Type, EntityMetadataContext> metadata = 
            new Dictionary<Type, EntityMetadataContext>();

        public static EntityMetadataContext<T> Bind<T>()
        {
            if (!metadata.TryGetValue(typeof(T), out var entityMetadata))
            {
                entityMetadata = new EntityMetadataContext<T>();
                metadata.Add(typeof(T), entityMetadata);
            }

            return (EntityMetadataContext<T>)entityMetadata;
        }
    }

    public abstract class EntityMetadataContext
    {
        public Type Type { get; protected set; }

        public Dictionary<string, EntityMetadataProp> Properties { get; private set; } =
            new Dictionary<string, EntityMetadataProp>();

        public Dictionary<string, EntityMetadataProp> Included { get; private set; } =
            new Dictionary<string, EntityMetadataProp>();

        public Dictionary<string, EntityMetadataProp> Keys { get; private set; } =
            new Dictionary<string, EntityMetadataProp>();

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
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
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

        public EntityMetadataContext<T> Key(params Expression<Func<T, object>>[] fields)
        {
            foreach (var field in fields)
            {
                if (Properties.TryGetValue(GetPropertyName(field), out var prop))
                {
                    Keys.Add(prop.Name, prop);
                }
                else
                {
                    throw new InvalidOperationException("Invalid expression");
                }
            }

            return this;
        }

        public EntityMetadataContext<T> Ignore(params Expression<Func<T, object>>[] fields)
        {
            foreach (var field in fields)
            {
                Included.Remove(GetPropertyName(field));
            }

            return this;
        }

        private static string GetPropertyName(Expression<Func<T, object>> expression)
        {
            var info = GetMemberInfo(expression);
            return info.Member.Name;
        }

        private static MemberExpression GetMemberInfo(Expression method)
        {
            LambdaExpression lambda = method as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("method");

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr =
                    ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException("method");

            return memberExpr;
        }
    }

    public class EntityMetadataProp
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PropertyInfo Info { get; set; }
    }
}
