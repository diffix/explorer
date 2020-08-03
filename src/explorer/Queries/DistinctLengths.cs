namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class DistinctLengths :
        DQuery,
        DResultParser<ValueWithCount<JsonElement>>
    {
        protected override string GetQueryStatement(string table, string column)
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