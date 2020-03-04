namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Aircloak.JsonApi.JsonReaderExtensions;

    internal class CyclicalDatetimes :
        IQuerySpec<CyclicalDatetimes.Result>
    {
        public CyclicalDatetimes(
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
                    year({ColumnName}),
                    quarter({ColumnName}),
                    month({ColumnName}),
                    day({ColumnName}),
                    hour({ColumnName}),
                    minute({ColumnName}),
                    second({ColumnName}),
                    weekday({ColumnName}),
                    count(*),
                    count_noise(*)
                from {TableName}
                group by grouping sets (1, 2, 3, 4, 5, 6, 7, 8)
                ";
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) =>
            new Result
            {
                Year = reader.ParseAircloakResultValue<int>(),

                Quarter = reader.ParseAircloakResultValue<int>(),

                Month = reader.ParseAircloakResultValue<int>(),

                Day = reader.ParseAircloakResultValue<int>(),

                Hour = reader.ParseAircloakResultValue<int>(),

                Minute = reader.ParseAircloakResultValue<int>(),

                Second = reader.ParseAircloakResultValue<int>(),

                Weekday = reader.ParseAircloakResultValue<int>(),

                Count = reader.ParseCount(),

                CountNoise = reader.ParseCountNoise(),
            };

#pragma warning disable CS8618 // Non-nullable property 'Year' is uninitialized. Consider declaring the property as nullable. 
        public class Result
        {
            public AircloakValue<int> Year { get; set; }

            public AircloakValue<int> Quarter { get; set; }

            public AircloakValue<int> Month { get; set; }

            public AircloakValue<int> Day { get; set; }

            public AircloakValue<int> Hour { get; set; }

            public AircloakValue<int> Minute { get; set; }

            public AircloakValue<int> Second { get; set; }

            public AircloakValue<int> Weekday { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
#pragma warning restore CS8618 // Non-nullable property 'X' is uninitialized. Consider declaring the property as nullable. 
    }
}