namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class DistinctColumnValues : DQuery<ValueWithCount<JsonElement>>
    {
        public string GetQueryStatement(string table, string column)
        {
            return $@"
                select
                    {column},
                    count(*),
                    count_noise(*)
                from {table}
                group by {column}";
        }

        public ValueWithCount<JsonElement> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<JsonElement>(ref reader);
    }
}