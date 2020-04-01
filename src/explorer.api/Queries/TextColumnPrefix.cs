namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.JsonConversion;
    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Interfaces;

    internal class TextColumnPrefix :
        IQuerySpec<TextColumnPrefix.Result>
    {
        public TextColumnPrefix(string tableName, string columnName, int length)
        {
            // TODO: determine prefix length dynamically
            TableName = tableName;
            ColumnName = columnName;
            Length = length;
        }

        public string QueryStatement => $@"
select 
    left({ColumnName}, {Length}),
    count(*),
    count_noise(*)
from {TableName}
group by 1
having length(left({ColumnName}, {Length})) = {Length}";

        private string TableName { get; }

        private string ColumnName { get; }

        private int Length { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result : ICountAggregate, INullable, ISuppressible
        {
            private readonly AircloakValue<string> prefixColumn;

            public Result(ref Utf8JsonReader reader)
            {
                prefixColumn = reader.ParseAircloakResultValue<string>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public string Prefix => prefixColumn.HasValue ? prefixColumn.Value : string.Empty;

            public long Count { get; set; }

            public double? CountNoise { get; set; }

            public bool IsNull => prefixColumn.IsNull;

            public bool IsSuppressed => prefixColumn.IsSuppressed;

            public bool HasValue => prefixColumn.HasValue;
        }
    }
}