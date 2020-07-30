namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class DistinctLengths : DQuery<ValueWithCount<JsonElement>>
    {
        public string BuildQueryStatement(DSqlObjectName table, DSqlObjectName column)
        {
            return $@"
                select
                    length({column}),
                    count(*),
                    count_noise(*)
                from {table}
                group by {column}";
        }

        public ValueWithCount<JsonElement> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<JsonElement>(ref reader);
    }
}