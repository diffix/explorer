namespace Explorer.Explorers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class EmailColumnExplorer : ExplorerBase
    {
        public const string EmailAddressChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";

        public EmailColumnExplorer(DQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore()
        {
            var emailCheckQ = await ResolveQuery(
                new TextColumnTrim(TableName, ColumnName, TextColumnTrimType.Both, EmailAddressChars));

            var counts = ValueCounts.Compute(emailCheckQ.Rows);

            var isEmail = counts.TotalCount == emailCheckQ.Rows
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            PublishMetric(new UntypedMetric(name: "is_email", metric: isEmail));

            if (!isEmail)
            {
                return;
            }

            var tldQ = await ResolveQuery(
                new TextColumnSuffix(TableName, ColumnName, 3, 7));

            var tldList =
                from row in tldQ.Rows
                where row.Value.StartsWith(".", StringComparison.InvariantCulture)
                orderby row.Count descending
                select new
                {
                    name = row.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "email.top_level_domains", metric: tldList));

            var domainQ = await ResolveQuery(
                new TextColumnTrim(TableName, ColumnName, TextColumnTrimType.Leading, EmailAddressChars));

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
