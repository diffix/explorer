namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.JsonConversion;
    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Interfaces;

    internal class TextColumnSuffix :
        IQuerySpec<TextColumnSuffix.Result>
    {
        public TextColumnSuffix(string tableName, string columnName, int minLength, int maxLength)
        {
            // TODO: determine suffix length dynamically
            var indexes = Enumerable.Range(minLength, maxLength - minLength + 1);
            TableName = tableName;
            ColumnNames = string.Join(", ", indexes.Select(i => $"s{i}"));
            SuffixExpressions = string.Join(",\n", indexes.Select(i => $"    right({columnName}, {i}) as s{i}"));
        }

        public string QueryStatement => $@"
select 
	concat({ColumnNames}) as suffix, 
    sum(count), 
    sum(count_noise)
from (
    select 
        {SuffixExpressions},
        count(*),
        count_noise(*)
    from {TableName}
    group by grouping sets ({ColumnNames})
    ) as suffixes
group by suffix
order by sum(count) desc";

        private string TableName { get; }

        private string ColumnNames { get; }

        private string SuffixExpressions { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result : ICountAggregate, INullable, ISuppressible
        {
            private readonly AircloakValue<string> suffixColumn;

            public Result(ref Utf8JsonReader reader)
            {
                suffixColumn = reader.ParseAircloakResultValue<string>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public string Suffix => suffixColumn.HasValue ? suffixColumn.Value : string.Empty;

            public long Count { get; set; }

            public double? CountNoise { get; set; }

            public bool IsNull => suffixColumn.IsNull;

            public bool IsSuppressed => suffixColumn.IsSuppressed;
        }
    }
}