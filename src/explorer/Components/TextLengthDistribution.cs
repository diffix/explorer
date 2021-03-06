namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;
    using Microsoft.Extensions.Options;

    public class TextLengthDistribution
        : ExplorerComponent<TextLengthDistribution.Result>, PublisherComponent
    {
        private readonly ExplorerOptions options;

        public TextLengthDistribution(IOptions<ExplorerOptions> options)
        {
            this.options = options.Value;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric("text.length.values", result.Distribution
                .Select(item => new { Value = item.Length, item.Count })
                .ToList());

            if (result.ValueCounts != null)
            {
                yield return new UntypedMetric("text.length.counts", result.ValueCounts);
            }
        }

        internal async Task<Result> ComputeIsolatorLengthDistribution()
        {
            var distribution = new List<(long Length, long Count)>();
            var pos = 0;
            var oldCount = 0L;
            while (pos <= options.TextColumnMaxExplorationLength)
            {
                var columnsCount = Math.Min(options.SubstringQueryColumnCount, options.TextColumnMaxExplorationLength + 1 - pos);
                var query = new TextColumnSubstring(pos, 1, columnsCount, 0);
                var qresult = await Context.Exec(query);
                var rows = qresult.Rows.OrderBy(r => r.Index).ToList();
                if (rows.Count > 0 && rows.All(r => r.Count == oldCount))
                {
                    break;
                }

                foreach (var row in rows)
                {
                    if (row.Count > oldCount)
                    {
                        distribution.Add((Length: pos + row.Index, row.Count - oldCount));
                    }
                    oldCount = row.Count;
                }
                pos += columnsCount;
            }
            return new Result(distribution);
        }

        protected async Task<Result> ComputeNonIsolatorLengthDistribution()
        {
            var distinctResult = await Context.Exec(new DistinctLengths());
            return new Result(distinctResult.Rows);
        }

        protected override async Task<Result?> Explore()
        {
            return Context.ColumnInfo.Isolating ?
                await ComputeIsolatorLengthDistribution() :
                await ComputeNonIsolatorLengthDistribution();
        }

        public class Result
        {
            internal Result(IList<(long, long)> distribution)
            {
                Distribution = distribution;
            }

            internal Result(IEnumerable<ValueWithCount<JsonElement>> distinctRows)
            {
                ValueCounts = ValueCounts.Compute(distinctRows);
                Distribution = distinctRows
                    .Where(r => r.HasValue)
                    .OrderBy(r => r.Value.GetInt32())
                    .Select(r => (r.Value.GetInt64(), r.Count))
                    .ToList();
            }

            internal ValueCounts? ValueCounts { get; }

            internal IList<(long Length, long Count)> Distribution { get; }
        }
    }
}
