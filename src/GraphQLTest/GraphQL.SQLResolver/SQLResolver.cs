using Dapper;
using GraphQL.Language.AST;
using GraphQL.POCO;
using GraphQLTest;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.SQLResolver
{
    public static class IHaveSelectionSetExtensions
    {
        public static IDictionary<string, Field> GetSelectedFields(this IHaveSelectionSet selectionSet)
        {
            if (selectionSet != null)
            {
                var fields = selectionSet
                    .SelectionSet
                    .Selections
                    .OfType<Field>()
                    .ToDictionary(field => field.Name);

                return fields;
            }
            return null;
        }
    }

    public enum SQLOperation
    { 
        SELECT,
        JOIN
    }

    public class SQLResolver : IGraphQLResolver
    {
        private readonly Func<IDbConnection> connectionFactory;

        public SQLResolver(Func<IDbConnection> connectionFactory)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static string GetAlias(int level) => $"{(char)(level + 'a')}";

        private static string ForeignKeyCriteria(string leftAlias, string rightAlias, EntityMetadataRelation relation)
        {
            List<string> criteria = new List<string>();

            foreach (var keyField in relation.EntityRightForeignKeys)
            {
                criteria.Add($"{leftAlias}.[{keyField.Key}] = {rightAlias}.[{keyField.Value}]");
            }

            return string.Join(" AND ", criteria);
        }

        internal class SqlGenerationContext
        {
            public string Sql { get; set; } = "";
            public List<dynamic> SelectFields { get; set; } = new List<dynamic>();
            public List<Type> SplitOnTypes { get; set; } = new List<Type>();
            public List<string> SplitOnFields { get; set; } = new List<string>();
            public List<EntityMetadataRelation> SplitOnRelations { get; set; } = new List<EntityMetadataRelation>();
        }

        private static SqlGenerationContext GenerateSQL(
            IHaveSelectionSet context, 
            EntityMetadataContext metadata,
            SqlGenerationContext sqlContext = null,
            SQLOperation operation = SQLOperation.SELECT,
            int level = 0,
            string parentAlias = null,
            EntityMetadataRelation relationMetadata = null)
        {
            if (sqlContext == null)
            {
                sqlContext = new SqlGenerationContext();
            }

            var key = metadata.Keys.Values.ToArray();

            var schema = 
                metadata.CustomMetadata.TryGetValue(Globals.CUSTOM_METADATA_SCHEMA, out var customSchema) ? 
                    customSchema : "dbo";

            var table = 
                metadata.CustomMetadata.TryGetValue(Globals.CUSTOM_METADATA_TABLE, out var customTable) ? 
                    customTable : metadata.Type.Name;

            var queriedFields = context.GetSelectedFields();

            var alias = GetAlias(level);

            var entityFields =
                metadata.Included.Keys
                    .Select(f => f.ToLower())
                    .Intersect(queriedFields.Keys)
                    .Select(f => new { field = $"{f}", exp = $"{alias}.[{f}]" })
                    .ToArray();

            sqlContext.SelectFields.AddRange(entityFields);
            sqlContext.SplitOnTypes.Add(metadata.Type);
            if (level > 0)
            {
                sqlContext.SplitOnFields.Add(entityFields.First().field);
            }

            if (operation == SQLOperation.SELECT)
            {
                sqlContext.Sql = $"SELECT %fields% FROM [{schema}].[{table}] {alias}";

                foreach (var field in queriedFields)
                {
                    if (metadata.Relations.TryGetValue(field.Key.ToLower(), out var relation))
                    {
                        sqlContext.SplitOnRelations.Add(relation);
                        GenerateSQL(
                            field.Value, 
                            relation.EntityRight, 
                            sqlContext, 
                            operation: SQLOperation.JOIN, 
                            level: level + 1,
                            parentAlias: alias,
                            relationMetadata: relation
                        );
                    }
                }
            }
            else if (operation == SQLOperation.JOIN)
            {
                sqlContext.Sql += $" LEFT JOIN [{schema}].[{table}] {alias} ON {ForeignKeyCriteria(parentAlias, alias, relationMetadata)}";
            }

            if (level == 0)
            {
                sqlContext.Sql = sqlContext.Sql.Replace("%fields%", string.Join(",", sqlContext.SelectFields.Select(x => x.exp)));
            }

            return sqlContext;
        }

        public async Task<IEnumerable<dynamic>> GetAsync(
            IHaveSelectionSet context, 
            EntityMetadataContext metadata)
        {
            using (var connection = connectionFactory())
            {
                var sql = GenerateSQL(context, metadata);

                HashSet<object>[] mapping =
                    Enumerable.Range(1, sql.SplitOnTypes.Count())
                        .Select(x => new HashSet<object>())
                        .ToArray();

                List<dynamic>[] objectCache =
                    Enumerable.Range(1, sql.SplitOnTypes.Count())
                        .Select(x => new List<dynamic>())
                        .ToArray();

                object[] lastEntities = new dynamic[sql.SplitOnTypes.Count()];

                (await connection.QueryAsync<dynamic>(
                    sql.Sql,
                    sql.SplitOnTypes.ToArray(),
                    splitOn: string.Join(",", sql.SplitOnFields),
                    map: (entities) =>
                    {
                        object result = entities[0];

                        int i = 0;
                        foreach (var entity in entities)
                        {
                            var entityKey = 
                                i == 0 ? metadata.PrimaryKey(entity) : 
                                sql.SplitOnRelations[i - 1].EntityRight.PrimaryKey(entity);

                            if (mapping[i].Contains(entityKey))
                            {
                                if (i == 0)
                                {
                                    result = null;
                                }
                            }
                            else
                            {
                                if (lastEntities[i] != null)
                                {
                                    if (i < objectCache.Length - 1)
                                    {
                                        foreach (var obj in objectCache[i + 1])
                                        {
                                            sql.SplitOnRelations[i].Add(lastEntities[i], obj);
                                        }
                                        objectCache[i + 1].Clear();
                                    }
                                }

                                objectCache[i].Add(entity);
                                mapping[i].Add(entityKey);
                                lastEntities[i] = entity;
                            }

                            i++;
                        }

                        return result;
                    }
                )).ToArray();

                int idx = 0;
                foreach (var entity in lastEntities)
                {
                    if (entity != null)
                    {
                        if (idx < objectCache.Length - 1)
                        {
                            foreach (var obj in objectCache[idx + 1])
                            {
                                sql.SplitOnRelations[idx].Add(entity, obj);
                            }
                            objectCache[idx + 1].Clear();
                        }
                    }
                    idx++;
                }

                return objectCache[0];
            }
        }
    }
}
