namespace Explorer.Diffix.Extensions
{
    using System.Text.Json;

    using Aircloak.JsonApi.JsonReaderExtensions;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Queries;

    /// <summary>
    /// Extension methods for <see cref="Utf8JsonReader"/>.
    /// </summary>
    public static class JsonReaderExtensions
    {
        public static (int, AircloakValue<T>) ParseGroupingSet<T>(this ref Utf8JsonReader reader, int groupSize)
        {
            var groupingId = reader.ParseGroupingId();
            var converter = GroupingIdConverter.GetConverter(groupSize);
            AircloakValue<T>? resultEl = null;

            for (var i = 0; i < groupSize; i++)
            {
                if (converter.SingleIndexFromGroupingId(groupingId) == i)
                {
                    resultEl = reader.ParseAircloakResultValue<T>();
                }
                else
                {
                    reader.Read();
                }
            }

            return (
                groupingId,
                resultEl ?? throw new System.Exception("Unable to Parse result from grouping set."));
        }
    }
}