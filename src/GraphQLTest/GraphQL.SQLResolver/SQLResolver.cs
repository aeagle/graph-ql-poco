using Dapper;
using GraphQL.POCO;
using GraphQLTest;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.SQLResolver
{
    public class SQLResolver : IGraphQLResolver
    {
        private readonly Func<IDbConnection> connectionFactory;

        public SQLResolver(Func<IDbConnection> connectionFactory)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static string BaseSql(EntityMetadataContext metadata)
        {
            var schema = metadata.CustomMetadata.TryGetValue(Globals.CUSTOM_METADATA_SCHEMA, out var customSchema) ? customSchema : "dbo";
            var table = metadata.CustomMetadata.TryGetValue(Globals.CUSTOM_METADATA_TABLE, out var customTable) ? customTable : metadata.Type.Name;
            return $"SELECT {string.Join(",", metadata.Included.Keys.Select(f => $"[{f.ToLower()}]"))} FROM [{schema}].[{table}]";
        }

        private static IDictionary<string, object> PostProcess(dynamic row)
        {
            return
                ((IDictionary<string, object>)row)
                    .ToDictionary(key => key.Key, val => val.Value, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IEnumerable<object>> GetAllAsync(EntityMetadataContext metadata)
        {
            using (var connection = connectionFactory())
            {
                var sql = BaseSql(metadata);
                return (await connection.QueryAsync(sql)).Select(row => PostProcess(row));
            }
        }

        public async Task<object> GetByKeyAsync(
            EntityMetadataContext metadata, 
            params KeyValuePair<EntityMetadataProp, object>[] keyValues)
        {
            using (var connection = connectionFactory())
            {
                var sql = BaseSql(metadata);
                return PostProcess(await connection.QueryFirstAsync(sql));
            }
        }
    }
}
