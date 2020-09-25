namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Text.Json;

    public class CategoricalData
    {
        public CategoricalData(ValuesListType values, ValueCounts valueCounts)
        {
            Values = values;
            ValueCounts = valueCounts;
        }

        public ValuesListType Values { get; }

        public ValueCounts ValueCounts { get; }

        public class ValuesListType : List<ValueWithCount<JsonElement>>
        {
            public ValuesListType(IEnumerable<ValueWithCount<JsonElement>> items)
                : base(items)
            {
            }
        }
    }
}