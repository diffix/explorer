namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class EmailCheckComponent : ExplorerComponent<EmailCheckComponent.Result>, PublisherComponent
    {
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
            var emailCheck = await Context.Exec(
                new TextColumnTrim(TextColumnTrimType.Both, Constants.EmailAddressChars));
            var isEmail = emailCheck.Rows.All(r => r.IsNull || (!r.IsSuppressed && r.Value == "@"));
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
