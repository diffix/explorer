namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class EmailCheckComponent : ExplorerComponent<bool>, PublisherComponent
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

        protected override Task<bool> Explore() => CheckIsEmail(conn, ctx);

        private static async Task<bool> CheckIsEmail(DConnection conn, ExplorerContext ctx)
        {
            var emailCheck = await conn.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Both, TextUtilities.EmailAddressChars));

            return emailCheck.Rows.All(r => r.IsNull || (!r.IsSuppressed && r.Value == "@"));
        }
    }
}