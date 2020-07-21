namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class DistinctColumnValues : DQuery<ValueWithCount<JsonElement>>
    {
        public DistinctColumnValues(DSqlObjectName tableName, string expression)
        {
            QueryStatement = $@"
                select
                    {expression},
                    count(*),
                    count_noise(*)
                from {tableName}
                group by {expression}";
        }

        public DistinctColumnValues(DSqlObjectName tableName, DSqlObjectName columnName)
        : this(tableName, columnName.ToString())
        {
        }

        public string QueryStatement { get; }

        public ValueWithCount<JsonElement> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<JsonElement>(ref reader);
    }
}