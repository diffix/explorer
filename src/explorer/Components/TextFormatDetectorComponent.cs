namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Queries;
    using TextFormat = Explorer.Metrics.TextFormat;

    public class TextFormatDetectorComponent : ExplorerComponent<TextFormatDetectorComponent.Result>
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;

        public TextFormatDetectorComponent(ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
        }

        protected override async Task<Result?> Explore()
        {
            var distinctValuesResult = await distinctValuesProvider.ResultAsync;
            if (distinctValuesResult == null)
            {
                return null;
            }

            var emailCheck = await Context.Exec(new EmailCheck());
            var emailCount = emailCheck.Rows.First();
            if (emailCount >= distinctValuesResult.ValueCounts.NonSuppressedNonNullCount)
            {
                return new Result(TextFormat.Email);
            }

            return new Result(TextFormat.Unknown);
        }

        public class Result
        {
            public Result(TextFormat value)
            {
                TextFormat = value;
            }

            public TextFormat TextFormat { get; }
        }
    }
}
