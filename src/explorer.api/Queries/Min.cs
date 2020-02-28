namespace Explorer.Queries
{
    using System.Text.Json;
    using Aircloak.JsonApi;

    internal class Min :
        IQuerySpec<Min.Result>
    {
        public Min(string tableName, string columnName, decimal? upperBound = null)
        {
            TableName = tableName;
            ColumnName = columnName;
            UpperBound = upperBound;
        }

        public string QueryStatement
        {
            get
            {
                var whereFragment = string.Empty;
                if (UpperBound.HasValue)
                {
                    whereFragment = $"where {ColumnName} between 0 and {UpperBound.Value}";
                }

                return $@"
                    select
                        min({ColumnName})
                    from {TableName}
                    {whereFragment}";
            }
        }

        private string TableName { get; }

        private string ColumnName { get; }

        private decimal? UpperBound { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Result { Min = null };
            }

            return new Result { Min = reader.GetDecimal() };
        }

        public class Result
        {
            public decimal? Min { get; set; }
        }
    }
}