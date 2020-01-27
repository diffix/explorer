#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1004 // Documentation line should begin with a space.
#pragma warning disable CA2227 // Change collection property to be read-only by removing the property setter
#pragma warning disable CA1815 // Struct type should override Equals
#pragma warning disable CA1034 // Do not nest types

namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the JSON response from a request to /api/queries/{query_id}.
    /// </summary>
    /// <typeparam name="TRow">The type that the query row will be deserialized to.</typeparam>
    public struct QueryResult<TRow>
    {
        public QueryResultInner<TRow> Query { get; set; }

        /// <summary>
        /// Gets the rows from the innards of the result type.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> that can be used to iterate over the rows.</returns>
        [JsonIgnore]
        public IEnumerable<TRow> ResultRows
        {
            get
            {
                foreach (var row_with_count in Query.Rows)
                {
                    yield return row_with_count.Row;
                }
            }
        }
    }

    /** <summary>
        Represents the contents of the inner 'query' item returned from /api/queries/{query_id}.

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
        <typeparam name="TRow">The type of contained rows.</typeparam>
    */
    public struct QueryResultInner<TRow>
    {
        public bool Completed { get; set; }

        public QueryDataSource DataSource { get; set; }

        public string Id { get; set; }

        public string QueryState { get; set; }

        public string Statement { get; set; }

        public IList<string> Columns { get; set; }

        public string Error { get; set; }

        public int RowCount { get; set; }

        public IList<AircloakType> Types { get; set; }

        public IList<QueryRowsWithCount> Rows { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData { get; set; }

        public struct QueryRowsWithCount
        {
            public TRow Row { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public struct QueryUser
        {
            public string Name { get; set; }
        }

        public struct QueryDataSource
        {
            public string Name { get; set; }
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1004 // Documentation line should begin with a space.
#pragma warning restore CA2227 // Change collection property to be read-only by removing the property setter
#pragma warning restore CA1815 // Struct type should override Equals
#pragma warning restore CA1034 // Do not nest types