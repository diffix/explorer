namespace Explorer.Components
{
    using System.Threading.Tasks;

    using Diffix;
    using Microsoft.Extensions.Options;

    public class SampleValuesGeneratorConfig : ExplorerComponent<SampleValuesGeneratorConfig.Result>
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ExplorerOptions options;

        public SampleValuesGeneratorConfig(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            IOptions<ExplorerOptions> options)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.options = options.Value;
        }

        public int SamplesToPublish => Context.SamplesToPublish;

        public double TextColumnMinFactorForCategoricalSampling => options.TextColumnMinFactorForCategoricalSampling;

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

            return new Result(categoricalSampling, minValuesForCategoricalSampling, SamplesToPublish);
        }

        public class Result
        {
            public Result(bool categoricalSampling, long minValuesForCategoricalSampling, int numValuesToPublish)
            {
                CategoricalSampling = categoricalSampling;
                MinValuesForCategoricalSampling = minValuesForCategoricalSampling;
                SamplesToPublish = numValuesToPublish;
            }

            public bool CategoricalSampling { get; }

            public long MinValuesForCategoricalSampling { get; }

            public int SamplesToPublish { get; }
        }
    }
}
