
namespace Explorer.Components
{
    using System.Collections.Generic;
    using Explorer.Metrics;

    public class TextLengthPublisher : PublisherComponent<TextLengthComponent.Result>
    {
        public TextLengthPublisher(ResultProvider<TextLengthComponent.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(TextLengthComponent.Result result)
        {
            if (result.Success)
            {
                var statsPublisher = new SimpleStatsPublisher<double>(result.Stats);

                result.Hist
                result.Quartiles


                return statsPublisher
            }
        }
    }
}