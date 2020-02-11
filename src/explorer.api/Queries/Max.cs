namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Explorer.Api.Models;

    internal class Max :
        IQuerySpec<Max.Result>
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

        public string TableName { get; }

        public string ColumnName { get; }

        public decimal? LowerBound { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Result { Max = null };
            }

            return new Result { Max = reader.GetDecimal() };
        }

        public class Result
        {
            public decimal? Max { get; set; }
        }
    }
}