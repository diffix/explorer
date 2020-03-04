namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Aircloak.JsonApi.JsonReaderExtensions;

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
            get => $@"
                select
                    date_trunc('year', {ColumnName}) as year,
                    date_trunc('quarter', {ColumnName}) as quarter,
                    date_trunc('month', {ColumnName}) as month,
                    date_trunc('day', {ColumnName}) as day,
                    date_trunc('hour', {ColumnName}) as hour,
                    date_trunc('minute', {ColumnName}) as minute,
                    date_trunc('second', {ColumnName}) as second,
                    count(*),
                    count_noise(*)
                from {TableName}
                group by grouping sets (1, 2, 3, 4, 5, 6, 7)
                ";
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) =>
            new Result
            {
                Year = reader.ParseAircloakResultValue((ref Utf8JsonReader reader) => reader.GetDateTime().Year),

                Quarter = reader.ParseAircloakResultValue<System.DateTime>(),

                Month = reader.ParseAircloakResultValue((ref Utf8JsonReader reader) => reader.GetDateTime().Month),

                Day = reader.ParseAircloakResultValue((ref Utf8JsonReader reader) => reader.GetDateTime().Day),

                Hour = reader.ParseAircloakResultValue((ref Utf8JsonReader reader) => reader.GetDateTime().Hour),

                Minute = reader.ParseAircloakResultValue((ref Utf8JsonReader reader) => reader.GetDateTime().Minute),

                Second = reader.ParseAircloakResultValue((ref Utf8JsonReader reader) => reader.GetDateTime().Second),

                Count = reader.ParseCount(),

                CountNoise = reader.ParseCountNoise(),
            };

#pragma warning disable CS8618 // Non-nullable property 'Year' is uninitialized. Consider declaring the property as nullable. 
        public class Result
        {
            public AircloakValue<int> Year { get; set; }

            public AircloakValue<System.DateTime> Quarter { get; set; }

            public AircloakValue<int> Month { get; set; }

            public AircloakValue<int> Day { get; set; }

            public AircloakValue<int> Hour { get; set; }

            public AircloakValue<int> Minute { get; set; }

            public AircloakValue<int> Second { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
#pragma warning restore CS8618 // Non-nullable property 'X' is uninitialized. Consider declaring the property as nullable. 
    }
}