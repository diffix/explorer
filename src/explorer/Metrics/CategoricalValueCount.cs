namespace Explorer.Metrics
{
    using System.Text.Json;

    public class CategoricalValueCount
    {
        public CategoricalValueCount(JsonElement value, long count)
        {
            Value = value;
            Count = count;
        }

        public JsonElement Value { get; }

        public long Count { get; }
    }
}