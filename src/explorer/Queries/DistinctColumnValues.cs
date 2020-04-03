namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class DistinctColumnValues : DQuery<ValueWithCount<JsonElement>>
    {
        public DistinctColumnValues(string tableName, string columnName)
        {
            QueryStatement = $@"
                select
                    {columnName},
                    count(*),
                    count_noise(*)
                from {tableName}
                group by {columnName}";
        }

        public string QueryStatement { get; }

        public ValueWithCount<JsonElement> ParseRow(ref Utf8JsonReader reader) =>
            ValueWithCount<JsonElement>.Parse(ref reader);
    }
}