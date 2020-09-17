namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.JsonConversion;

    internal class EmailCheck :
        DQuery<long>
    {
        public override long ParseRow(ref Utf8JsonReader reader) =>
            reader.ParseCount();

        protected override string GetQueryStatement(string table, string column)
        {
            return $@"
                select
                    count(*)
                from {table}
                where right({column}, 4) LIKE '%.%' AND {column} LIKE '%@%'";
        }
    }
}
