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
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;

        public EmailCheckComponent(DConnection conn, ExplorerContext ctx)
        {
            this.conn = conn;
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            yield return new UntypedMetric(name: "is_email", metric: await ResultAsync);
        }

        protected override Task<Result> Explore() => CheckIsEmail(conn, ctx);

        private static async Task<Result> CheckIsEmail(DConnection conn, ExplorerContext ctx)
        {
            var emailCheck = await conn.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Both, Constants.EmailAddressChars));
            var isEmail = emailCheck.Rows.All(r => r.IsNull || (!r.IsSuppressed && r.Value == "@"));
            return new Result(isEmail);
        }

        public class Result
        {
            public Result(bool value)
            {
                Value = value;
            }

            public bool Value { get; }
        }
    }
}
