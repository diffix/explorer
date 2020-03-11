namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.JsonReaderExtensions;
    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Interfaces;

    internal class BucketedDatetimes :
        IQuerySpec<BucketedDatetimes.Result>
    {
        public BucketedDatetimes(
            string tableName,
            string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string QueryStatement
        {
            get
            {
                var fragment = $@"
                    date_trunc('year', {ColumnName}),
                    date_trunc('quarter', {ColumnName}),
                    date_trunc('month', {ColumnName}),
                    date_trunc('day', {ColumnName}),
                    date_trunc('hour', {ColumnName}),
                    date_trunc('minute', {ColumnName}),
                    date_trunc('second', {ColumnName})";

                return $@"
                select
                    grouping_id(
                        {fragment}
                    ),
                    {fragment},
                    count(*),
                    count_noise(*)
                from {TableName}
                group by grouping sets (1, 2, 3, 4, 5, 6, 7)
                ";
            }
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) =>
            new Result
            {
                GroupingId = reader.ParseGroupingId(),

                Year = reader.ParseAircloakResultValue<System.DateTime>(),

                Quarter = reader.ParseAircloakResultValue<System.DateTime>(),

                Month = reader.ParseAircloakResultValue<System.DateTime>(),

                Day = reader.ParseAircloakResultValue<System.DateTime>(),

                Hour = reader.ParseAircloakResultValue<System.DateTime>(),

                Minute = reader.ParseAircloakResultValue<System.DateTime>(),

                Second = reader.ParseAircloakResultValue<System.DateTime>(),

                Count = reader.ParseCount(),

                CountNoise = reader.ParseCountNoise(),
            };

#pragma warning disable CS8618 // Non-nullable property 'Year' is uninitialized. Consider declaring the property as nullable.
        public class Result : ICountAggregate
        {
            public int GroupingId { get; set; }

            public AircloakValue<System.DateTime> Year { get; set; }

            public AircloakValue<System.DateTime> Quarter { get; set; }

            public AircloakValue<System.DateTime> Month { get; set; }

            public AircloakValue<System.DateTime> Day { get; set; }

            public AircloakValue<System.DateTime> Hour { get; set; }

            public AircloakValue<System.DateTime> Minute { get; set; }

            public AircloakValue<System.DateTime> Second { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
#pragma warning restore CS8618 // Non-nullable property 'X' is uninitialized. Consider declaring the property as nullable.
    }
}