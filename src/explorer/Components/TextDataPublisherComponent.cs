namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class TextDataPublisherComponent : PublisherComponent
    {
        private readonly ResultProvider<TextFormatDetectorComponent.Result> textFormatProvider;
        private readonly ResultProvider<TextLengthDistributionComponent.Result> textLengthDistributionProvider;

        public TextDataPublisherComponent(
            ResultProvider<TextFormatDetectorComponent.Result> textFormatProvider,
            ResultProvider<TextLengthDistributionComponent.Result> textLengthDistributionProvider)
        {
            this.textFormatProvider = textFormatProvider;
            this.textLengthDistributionProvider = textLengthDistributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var textFormatResult = await textFormatProvider.ResultAsync;
            if (textFormatResult == null)
            {
                yield break;
            }

            var lengthDistributionResult = await textLengthDistributionProvider.ResultAsync;
            if (lengthDistributionResult == null)
            {
                yield break;
            }

            var lengthDistribution = new TextData.LengthsDistributionType(
                    lengthDistributionResult.Distribution.Select(item => new ValueWithCount<long>(item.Value, item.Count)));

            var textData = new TextData(textFormatResult.TextFormat, lengthDistribution, lengthDistributionResult.ValueCounts);

            yield return ExploreMetric.Create(MetricDefinitions.TextData, textData);
        }
    }
}
