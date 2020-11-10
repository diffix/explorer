namespace Explorer.Queries
{
    using System;
    using System.Text.Json;

    public class DatetimeProjection : ColumnProjection
    {
        private readonly string dateInterval;
        private readonly Random rng = new Random();

        public DatetimeProjection(string column, int index, string dateInterval)
        : base(column, index)
        {
            this.dateInterval = dateInterval;
        }

        public override string Project()
        {
            return $"date_trunc('{dateInterval}', {Column})";
        }

        public override object? Invert(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            var lowerBound = value.GetDateTime();

            return dateInterval switch
            {
                // Add a random offset at the next lower level of granularity.
                "year" => lowerBound.AddMonths(rng.Next(12)),
                "quarter" => lowerBound.AddMonths(rng.Next(3)),
                "month" => lowerBound.AddDays(rng.NextDouble() * DateTime.DaysInMonth(lowerBound.Year, lowerBound.Month)),
                "day" => lowerBound.AddHours(rng.NextDouble() * 24),
                "hour" => lowerBound.AddMinutes(rng.NextDouble() * 60),
                "minute" => lowerBound.AddSeconds(rng.NextDouble() * 60),
                "second" => lowerBound.AddMilliseconds(rng.NextDouble() * 1000),
                _ => lowerBound,
            };
        }
    }
}