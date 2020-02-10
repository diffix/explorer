namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Explorer.Api.Models;

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

        public string TableName { get; }

        public string ColumnName { get; }

        public decimal? UpperBound { get; }

        public class Result : IJsonArrayConvertible
        {
            public decimal? Min { get; set; }

            public void FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return;
                }

                Min = reader.GetDecimal();
            }
        }
    }
}