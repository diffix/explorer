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
        private readonly ExplorerContext ctx;

        public EmailCheckComponent(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            yield return new UntypedMetric(name: "is_email", metric: await ResultAsync);
        }

        protected override Task<Result> Explore() => CheckIsEmail(ctx);

        private static async Task<Result> CheckIsEmail(ExplorerContext ctx)
        {
            var emailCheck = await ctx.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Both, Constants.EmailAddressChars));
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
