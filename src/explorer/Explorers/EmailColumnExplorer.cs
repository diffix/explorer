namespace Explorer.Explorers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class EmailColumnExplorer : ExplorerBase<ColumnExplorerContext>
    {
        public const string EmailAddressChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";

        public override async Task Explore(DConnection conn, ColumnExplorerContext ctx)
        {
            var emailCheckQ = await conn.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Both, EmailAddressChars));

            var counts = ValueCounts.Compute(emailCheckQ.Rows);

            var isEmail = counts.TotalCount == emailCheckQ.Rows
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            PublishMetric(new UntypedMetric(name: "is_email", metric: isEmail));

            if (!isEmail)
            {
                return;
            }

            var tldQ = await conn.Exec(
                new TextColumnSuffix(ctx.Table, ctx.Column, 3, 7));

            var tldList =
                from row in tldQ.Rows
                where row.HasValue && row.Value.StartsWith(".", StringComparison.InvariantCulture)
                orderby row.Count descending
                select new
                {
                    name = row.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "email.top_level_domains", metric: tldList));

            var domainQ = await conn.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Leading, EmailAddressChars));

            var domainList =
                from row in domainQ.Rows
                where !row.IsSuppressed && !row.IsNull
                orderby row.Count descending
                select new
                {
                    name = row.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "email.domains", metric: domainList.Take(10)));
        }
    }
}
