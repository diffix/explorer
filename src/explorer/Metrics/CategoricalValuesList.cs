namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Text.Json;

    public class CategoricalValuesList : List<ValueWithCount<JsonElement>>
    {
        public CategoricalValuesList(IEnumerable<ValueWithCount<JsonElement>> items)
            : base(items)
        {
        }
    }
}