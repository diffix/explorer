namespace Explorer.Components
{
    using System.Threading.Tasks;

    using Diffix;

    public class SampleValuesGeneratorConfig : ExplorerComponent<SampleValuesGeneratorConfig.Result>
    {
        public const int DefaultNumValuesToPublish = 20;
        public const double DefaultTextColumnMinFactorForCategoricalSampling = 0.95;

        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;

        public SampleValuesGeneratorConfig(ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
        }

        public int NumValuesToPublish { get; set; } = DefaultNumValuesToPublish;

        public double TextColumnMinFactorForCategoricalSampling { get; set; } = DefaultTextColumnMinFactorForCategoricalSampling;

        protected override async Task<Result?> Explore()
        {
            var distinctValuesResult = await distinctValuesProvider.ResultAsync;
            if (distinctValuesResult == null)
            {
                return null;
            }

            var valueCounts = distinctValuesResult.ValueCounts;
            var categoricalSampling = valueCounts.IsCategorical;
            var minValuesForCategoricalSampling = (long)(valueCounts.TotalCount * TextColumnMinFactorForCategoricalSampling);

            // for the case of text columns
            // the sample data generation algorithm involving substrings is quite imprecise
            // so we use a relaxed condition for when to do sampling directly from the available values
            // (the default value for the ratio is intentionally quite small)
            if (Context.ColumnInfo.Type == DValueType.Text && valueCounts.NonSuppressedCount > minValuesForCategoricalSampling)
            {
                categoricalSampling = true;
            }

            return new Result(categoricalSampling, minValuesForCategoricalSampling, NumValuesToPublish);
        }

        public class Result
        {
            public Result(bool categoricalSampling, long minValuesForCategoricalSampling, int numValuesToPublish)
            {
                CategoricalSampling = categoricalSampling;
                MinValuesForCategoricalSampling = minValuesForCategoricalSampling;
                NumValuesToPublish = numValuesToPublish;
            }

            public bool CategoricalSampling { get; }

            public long MinValuesForCategoricalSampling { get; }

            public int NumValuesToPublish { get; }
        }
    }
}
