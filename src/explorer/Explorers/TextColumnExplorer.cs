namespace Explorer.Explorers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class TextColumnExplorer : ExplorerBase
    {
        private const double SuppressedRatioThreshold = 0.1;

        public TextColumnExplorer(DQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore()
        {
            var distinctValuesQ = await ResolveQuery(
                new DistinctColumnValues(TableName, ColumnName));

            var counts = ValueCounts.Compute(distinctValuesQ.Rows);

            PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: counts.SuppressedCount));

            // This shouldn't happen, but check anyway.
            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            PublishMetric(new UntypedMetric(name: "distinct.total_count", metric: counts.TotalCount));

            var distinctValueCounts =
                from row in distinctValuesQ.Rows
                where row.HasValue
                orderby row.Count descending
                select new
                {
                    row.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "distinct.top_values", metric: distinctValueCounts.Take(10)));

            if (counts.SuppressedCountRatio >= SuppressedRatioThreshold)
            {
                // we compute the common prefixes only if the row is not categorical
                await ExplorePrefixes();
            }
        }

        private async Task<IEnumerable<Prefix>> ExplorePrefixes()
        {
            var allPrefixes = new List<Prefix>();
            var length = 0;
            while (true)
            {
                length++;
                var prefixesQ = await ResolveQuery(
                    new TextColumnPrefix(TableName, ColumnName, length));

                var counts = ValueCounts.Compute(prefixesQ.Rows);
                var avgCount = (double)counts.NonSuppressedCount / counts.NonSuppressedRows;

                var prefixes =
                    from row in prefixesQ.Rows
                    let frequency = (double)row.Count / counts.NonSuppressedCount
                    where row.HasValue && row.Count > avgCount
                    orderby frequency descending
                    select new Prefix(row.Value, frequency);

                if (!prefixes.Any())
                {
                    break;
                }

                if (length > prefixes.Max(p => p.Value.Length))
                {
                    break;
                }

                allPrefixes.AddRange(prefixes);
            }

            var ret =
                from row in allPrefixes
                orderby row.Value.Length ascending, row.Frequency descending
                select row;

            PublishMetric(new UntypedMetric(name: "text.prefixes", metric: ret));

            return ret;
        }

        private struct Prefix
        {
            public Prefix(string value, double frequency)
            {
                Value = value;
                Frequency = frequency;
            }

            public string Value { get; }

            public double Frequency { get; }
        }
    }
}
