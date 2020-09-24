namespace Explorer.Metrics
{
    using System.Collections.Generic;

    public class TextLengthDistribution : List<ValueWithCount<long>>
    {
        public TextLengthDistribution(IEnumerable<ValueWithCount<long>> items)
            : base(items)
        {
        }
    }
}
