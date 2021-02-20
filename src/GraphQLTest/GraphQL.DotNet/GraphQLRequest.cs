using GraphQL.SystemTextJson;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraphQLTest
{
    public class GraphQLRequest
    {
        public string OperationName { get; set; }

        public string Query { get; set; }

        [JsonConverter(typeof(ObjectDictionaryConverter))]
        public Dictionary<string, object> Variables { get; set; }
    }
}
