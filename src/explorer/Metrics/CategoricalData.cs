namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Text.Json;

    public class CategoricalData
    {
        public CategoricalData(IReadOnlyList<ValueWithCount<JsonElement>> values, ValueCounts valueCounts)
        {
            Values = values;
            ValueCounts = valueCounts;
        }

        public IReadOnlyList<ValueWithCount<JsonElement>> Values { get; }

        public ValueCounts ValueCounts { get; }
    }
}