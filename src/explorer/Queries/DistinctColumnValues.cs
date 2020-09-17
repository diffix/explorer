namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.Utils;

    internal class DistinctColumnValues :
        DQuery<ValueWithCount<JsonElement>>
    {
        public override ValueWithCount<JsonElement> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<JsonElement>(ref reader);

        protected override string GetQueryStatement(string table, string column)
        {
            return $@"
                select
                    {column},
                    count(*),
                    count_noise(*)
                from {table}
                group by {column}";
        }
    }
}