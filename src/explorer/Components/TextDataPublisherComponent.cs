namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class TextDataPublisherComponent : ExplorerComponent<TextData>, PublisherComponent
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
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }
            yield return ExploreMetric.Create(MetricDefinitions.TextData, result);
        }

        protected async override Task<TextData?> Explore()
        {
            var textFormatResult = await textFormatProvider.ResultAsync;
            if (textFormatResult == null)
            {
                return null;
            }

            var lengthDistributionResult = await textLengthDistributionProvider.ResultAsync;
            if (lengthDistributionResult == null)
            {
                return null;
            }

            var lengthDistribution = new TextData.LengthsDistributionType(
                    lengthDistributionResult.Distribution.Select(item => new ValueWithCount<long>(item.Value, item.Count)));
            return new TextData(textFormatResult.TextFormat, lengthDistribution, lengthDistributionResult.ValueCounts);
        }
    }
}
