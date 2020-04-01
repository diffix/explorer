namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;

    internal class Max :
        IQuerySpec<Max.Result<long>>,
        IQuerySpec<Max.Result<double>>,
        IQuerySpec<Max.Result<decimal>>
    {
        public Max(string tableName, string columnName, decimal? lowerBound = null)
        {
            TableName = tableName;
            ColumnName = columnName;
            LowerBound = lowerBound;
        }

        public string QueryStatement
        {
            get
            {
                var whereFragment = string.Empty;
                if (LowerBound.HasValue)
                {
                    whereFragment = $"where {ColumnName} between {LowerBound.Value} and {LowerBound.Value * 2}";
                }

                return $@"
                    select
                        max({ColumnName})
                    from {TableName}
                    {whereFragment}";
            }
        }

        private string TableName { get; }

        private string ColumnName { get; }

        private decimal? LowerBound { get; }

        Result<long> IQuerySpec<Result<long>>.FromJsonArray(ref Utf8JsonReader reader) =>
            new Result<long>
            {
                Max = reader.ParseNullableMetric<long>(),
            };

        Result<double> IQuerySpec<Result<double>>.FromJsonArray(ref Utf8JsonReader reader) =>
            new Result<double>
            {
                Max = reader.ParseNullableMetric<double>(),
            };

        Result<decimal> IQuerySpec<Result<decimal>>.FromJsonArray(ref Utf8JsonReader reader) =>
            new Result<decimal>
            {
                Max = reader.ParseNullableMetric<decimal>(),
            };

        public class Result<T>
            where T : struct
        {
            public T? Max { get; set; }
        }
    }
}