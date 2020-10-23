namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class TextLengthDistribution
        : ExplorerComponent<TextLengthDistribution.Result>, PublisherComponent
    {
        private const int DefaultSubstringQueryColumnCount = 5;

        public int SubstringQueryColumnCount { get; set; } = DefaultSubstringQueryColumnCount;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric("text.length.values", result.Distribution
                .Select(item => new { item.Value, item.Count })
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
            while (true)
            {
                var query = new TextColumnSubstring(pos, 1, SubstringQueryColumnCount, 0);
                var qresult = await Context.Exec(query);
                var rows = qresult.Rows.OrderBy(r => r.Index).ToList();
                if (rows.Count > 0 && rows.All(r => r.Count == oldCount))
                {
                    break;
                }

                foreach (var row in rows)
                {
                    if (row.Count != oldCount)
                    {
                        distribution.Add((Length: pos + row.Index, row.Count - oldCount));
                    }
                    oldCount = row.Count;
                }
                pos += SubstringQueryColumnCount;
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
                Distribution = ValueWithCountList<long>.FromTupleEnum(distribution);
            }

            internal Result(IEnumerable<ValueWithCount<JsonElement>> distinctRows)
            {
                ValueCounts = ValueCounts.Compute(distinctRows);
                Distribution = ValueWithCountList<long>.FromTupleEnum(distinctRows
                    .Where(r => r.HasValue)
                    .OrderBy(r => r.Value.GetInt32())
                    .Select(r => (r.Value.GetInt64(), r.Count)));
            }

            internal ValueCounts? ValueCounts { get; }

            internal ValueWithCountList<long> Distribution { get; }
        }
    }
}
