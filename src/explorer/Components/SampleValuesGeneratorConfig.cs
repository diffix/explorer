namespace Explorer.Components
{
    using System.Threading.Tasks;

    using Diffix;

    public class SampleValuesGeneratorConfig : ExplorerComponent<SampleValuesGeneratorConfig.Result>
    {
        public const int DefaultNumValuesToPublish = 20;
        public const int DefaultDistinctValuesBySamplesToPublishRatioThreshold = 5;

        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;

        public SampleValuesGeneratorConfig(ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
        }

        public int NumValuesToPublish { get; set; } = DefaultNumValuesToPublish;

        public int DistinctValuesBySamplesToPublishRatioThreshold { get; set; } = DefaultDistinctValuesBySamplesToPublishRatioThreshold;

        protected override async Task<Result?> Explore()
        {
            var distinctValuesResult = await distinctValuesProvider.ResultAsync;
            if (distinctValuesResult == null)
            {
                return null;
            }

            var valueCounts = distinctValuesResult.ValueCounts;
            var categoricalSampling = valueCounts.IsCategorical;
            var minRowsForCategoricalSampling = DistinctValuesBySamplesToPublishRatioThreshold * NumValuesToPublish;

            // for the case of text columns
            // the sample data generation algorithm involving substrings is quite imprecise
            // so we use a relaxed condition for when to do sampling directly from the available values
            // (the default value for the ratio is intentionally quite small)
            if (Context.ColumnInfo.Type == DValueType.Text && valueCounts.NonSuppressedRows > minRowsForCategoricalSampling)
            {
                categoricalSampling = true;
            }

            return new Result(categoricalSampling, minRowsForCategoricalSampling, NumValuesToPublish);
        }

        public class Result
        {
            public Result(bool categoricalSampling, int minRowsForCategoricalSampling, int numValuesToPublish)
            {
                CategoricalSampling = categoricalSampling;
                MinRowsForCategoricalSampling = minRowsForCategoricalSampling;
                NumValuesToPublish = numValuesToPublish;
            }

            public bool CategoricalSampling { get; }

            public int MinRowsForCategoricalSampling { get; }

            public int NumValuesToPublish { get; }
        }
    }
}
