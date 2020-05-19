
namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

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
                yield return new UntypedMetric("text.length.success", "true");

                yield return new UntypedMetric(
                    "text.length.histogram",
                    result.Histogram!.Buckets.Values.Select(b => new
                    {
                        Length = b.LowerBound,
                        b.Count,
                    }));

                yield return new UntypedMetric("text.length.quartiles", result.Quartiles!);
            }
            else
            {
                yield return new UntypedMetric("text.length.success", "false");
            }
        }
    }
}