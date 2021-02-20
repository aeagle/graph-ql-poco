using GraphQLTest;

namespace GraphQL.SQLResolver
{
    public static class EntityMetadataContextExtensions
    {
        public static EntityMetadataContext<T> Schema<T>(this EntityMetadataContext<T> context, string schemaName)
        {
            context.CustomMetadata.Add(Globals.CUSTOM_METADATA_SCHEMA, schemaName);
            return context;
        }
        public static EntityMetadataContext<T> Table<T>(this EntityMetadataContext<T> context, string tableName)
        {
            context.CustomMetadata.Add(Globals.CUSTOM_METADATA_TABLE, tableName);
            return context;
        }
    }
}
