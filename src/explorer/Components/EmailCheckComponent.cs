namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Queries;

    public class EmailCheckComponent : ExplorerComponent<EmailCheckComponent.Result>, PublisherComponent
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;

        public EmailCheckComponent(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric(name: "is_email", metric: result);
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
            var isEmail = emailCount >= distinctValuesResult.ValueCounts.NonSuppressedNonNullCount;
            return new Result(isEmail);
        }

        public class Result
        {
            public Result(bool value)
            {
                IsEmail = value;
            }

            public bool IsEmail { get; }
        }
    }
}
