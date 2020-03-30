namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Explorer.Diffix.Extensions;
    using Explorer.Queries;

    internal class EmailColumnExplorer : ExplorerBase
    {
        public const string EmailAddressChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";

        public EmailColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore(CancellationToken cancellationToken)
        {
            var emailCheckQ = await ResolveQuery<TextColumnTrim.Result>(
                new TextColumnTrim(TableName, ColumnName, TextColumnTrimType.Both, EmailAddressChars),
                cancellationToken);

            var (totalValueCount, suppressedValueCount) = emailCheckQ.ResultRows.CountTotalAndSuppressed();

            var isEmail = totalValueCount == emailCheckQ.ResultRows
                .Where(r => r.TrimmedText == "@" || r.IsNull)
                .Sum(r => r.Count);

            PublishMetric(new UntypedMetric(name: "is_email", metric: isEmail));

            if (!isEmail)
            {
                return;
            }

            var tldQ = await ResolveQuery<TextColumnSuffix.Result>(
                new TextColumnSuffix(TableName, ColumnName, 3, 7),
                cancellationToken);

            var tldList =
                from row in tldQ.ResultRows
                where row.Suffix.StartsWith(".", StringComparison.InvariantCulture)
                orderby row.Count descending
                select new
                {
                    name = row.Suffix,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "email.top_level_domains", metric: tldList));

            var domainQ = await ResolveQuery<TextColumnTrim.Result>(
                new TextColumnTrim(TableName, ColumnName, TextColumnTrimType.Leading, EmailAddressChars),
                cancellationToken);

            var domainList =
                from row in domainQ.ResultRows
                where !row.IsSuppressed && !row.IsNull
                orderby row.Count descending
                select new
                {
                    name = row.TrimmedText,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "email.domains", metric: domainList.Take(10)));
        }
    }
}
