namespace Explorer.Metrics
{
    using System.Collections.Generic;

    public class CategoricalValuesList : List<CategoricalValueCount>
    {
        public CategoricalValuesList(IEnumerable<CategoricalValueCount> enumerable)
            : base(enumerable)
        {
        }
    }
}