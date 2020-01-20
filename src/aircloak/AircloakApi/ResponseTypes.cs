namespace Explorer.Api.AircloakApi.ReponseTypes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    class AircloakTypeEnumConverter : JsonConverter<AircloakType>
    {
        public override AircloakType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "integer" => AircloakType.Integer,
                "real" => AircloakType.Real,
                "text" => AircloakType.Text,
                "timestamp" => AircloakType.Timestamp,
                "date" => AircloakType.Date,
                "datetime" => AircloakType.Datetime,
                "bool" => AircloakType.Bool,
                _ => AircloakType.Unknown,
            };
        }

        public override void Write(Utf8JsonWriter writer, AircloakType value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                AircloakType.Integer => "integer",
                AircloakType.Real => "real",
                AircloakType.Text => "text",
                AircloakType.Timestamp => "timestamp",
                AircloakType.Date => "date",
                AircloakType.Datetime => "datetime",
                AircloakType.Bool => "bool",
                _ => "unknown",
            };
            writer.WriteStringValue(s);
        }
    }

    /// <summary>
    /// Helper type representing the different data types an Aircloak column can take.
    /// </summary>
    [JsonConverter(typeof(AircloakTypeEnumConverter))]
    public enum AircloakType
    {
        Integer,
        Real,
        Text,
        Timestamp,
        Date,
        Datetime,
        Bool,
        Unknown
    }

    /// <summary>
    /// Helper type representing the JSON response from a request to /api/data_sources.
    /// </summary>
    public class DataSource
    {
        public struct Table
        {
            public struct Column
            {
                public string Name { get; set; }
                public AircloakType Type { get; set; }
            }
            public string Id { get; set; }
            public IEnumerable<Column> Columns { get; set; }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<Table> Tables { get; set; }
    }

    /** <summary>
        Helper type representing the contents of the inner 'query' item reurned from /api/queries/{query_id}.

        Example response: 
        <code>
        {
            &quot;query&quot;: {
                &quot;buckets_link&quot;: &quot;/queries/9c08137a-b69f-450c-a13c-383340ddda2c/buckets&quot;,
                &quot;completed&quot;: true,
                &quot;data_source&quot;: {
                    &quot;name&quot;: &quot;gda_banking&quot;
                },
                &quot;data_source_id&quot;: 9,
                &quot;id&quot;: &quot;9c08137a-b69f-450c-a13c-383340ddda2c&quot;,
                &quot;inserted_at&quot;: &quot;2020-01-15T13:42:09.255580&quot;,
                &quot;private_permalink&quot;: &quot;/permalink/private/query/[...],
                &quot;public_permalink&quot;: &quot;/permalink/public/query/[...],
                &quot;query_state&quot;: &quot;completed&quot;,
                &quot;session_id&quot;: null,
                &quot;statement&quot;: &quot;select count(*), count_noise(*) from loans&quot;,
                &quot;user&quot;: {
                    &quot;name&quot;: &quot;Daniel Lennon&quot;
                },
                &quot;columns&quot;: [
                    &quot;count&quot;,
                    &quot;count_noise&quot;
                ],
                &quot;error&quot;: null,
                &quot;info&quot;: [
                    &quot;[Debug] Using statistics-based anonymization.&quot;,
                    &quot;[Debug] Query executed in 0.255 seconds.&quot;
                ],
                &quot;log&quot;: &quot;2020-01-15 [...] [info] query finished\n&quot;,
                &quot;row_count&quot;: 1,
                &quot;types&quot;: [
                    &quot;integer&quot;,
                    &quot;real&quot;
                ],
                &quot;rows&quot;: [
                {
                    &quot;unreliable&quot;: false,
                    &quot;row&quot;: [
                    825,
                    1
                    ],
                    &quot;occurrences&quot;: 1
                }
                ]
            }
        }
        </code>
        </summary>
        <typeparam name="RowType"></typeparam>
    */
    public struct QueryResultInner<RowType>
    {
        public struct QueryRowsWithCount
        {
            public RowType Row { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }
        public struct QueryUser
        {
            public string Name { get; set; }
        }

        public struct QueryDataSource
        {
            public string Name { get; set; }
        }
        public bool Completed { get; set; }
        public QueryDataSource DataSource { get; set; }
        public string Id { get; set; }
        public string QueryState { get; set; }
        public string Statement { get; set; }
        public List<string> Columns { get; set; }
        public string Error { get; set; }
        public int RowCount { get; set; }
        public List<AircloakType> Types { get; set; }
        public List<QueryRowsWithCount> Rows { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }


    /// <summary>
    /// Helper type representing the JSON response from a request to /api/queries/{query_id}.
    /// </summary>
    public struct QueryResult<RowType>
    {
        public QueryResultInner<RowType> Query { get; set; }
    }

    /// <summary>
    /// Helper type representing the JSON response from a POST request to /api/query.
    /// </summary>
    public struct QueryResponse
    {
        public bool Success { get; set; }
        public string QueryId { get; set; }
    }

    /// <summary>
    /// Helper type representing the JSON response from a request to /api/queries/{query_id}/cancel.
    /// </summary>
    public struct CancelResponse
    {
        public bool Success { get; set; }
    }
}