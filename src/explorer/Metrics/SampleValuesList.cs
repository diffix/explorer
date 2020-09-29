namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Linq;

    public abstract class SampleValuesList
    {
        protected SampleValuesList(IEnumerable<object> sampleValues)
        {
            SampleValues = sampleValues.ToList();
        }

        public IList<object> SampleValues { get; }
    }
}