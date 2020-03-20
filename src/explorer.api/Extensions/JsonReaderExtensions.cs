namespace Explorer.Diffix.Extensions
{
    using System.Text.Json;

    using Aircloak.JsonApi.JsonConversion;
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
            AircloakValue<T>? groupValue = null;

            for (var i = 0; i < groupSize; i++)
            {
                if (converter.SingleIndexFromGroupingId(groupingId) == i)
                {
                    groupValue = reader.ParseAircloakResultValue<T>();
                }
                else
                {
                    reader.Read();
                }
            }

            return (
                groupingId,
                groupValue ?? throw new System.Exception("Unable to Parse result from grouping set."));
        }
    }
}