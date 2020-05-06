namespace Explorer.Explorers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class TextColumnExplorer : ExplorerBase
    {
        public const string EmailAddressChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";
        private const double SuppressedRatioThreshold = 0.1;
        private const int SubstringQueryColumnCount = 5;
        private const int GeneratedValuesCount = 30;

        public override async Task Explore(DConnection conn, ExplorerContext ctx)
        {
            var distinctValuesQ = await conn.Exec(
                new DistinctColumnValues(ctx.Table, ctx.Column));

            var counts = ValueCounts.Compute(distinctValuesQ.Rows);

            PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: counts.SuppressedCount));

            // This shouldn't happen, but check anyway.
            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {ctx.Table}, {ctx.Column} is zero.");
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

            if (counts.SuppressedRowRatio > SuppressedRatioThreshold)
            {
                // we generate synthetic values if the row is not categorical
                var isEmail = await CheckIsEmail(conn, ctx);
                PublishMetric(new UntypedMetric(name: "is_email", metric: isEmail));
                var values = isEmail ? await GenerateEmails(conn, ctx) : await GenerateStrings(conn, ctx);
                PublishMetric(new UntypedMetric(name: "synthetic_values", metric: values));
            }
        }

        private static async Task<IEnumerable<string>> GenerateStrings(DConnection conn, ExplorerContext ctx)
        {
            // the substring lengths 3 and 4 were determined empirically to work for column containing names
            var substrings = new SubstringDataCollection(maxSubstringLength: 4);
            await ExploreSubstrings(conn, ctx, 3, substrings);
            await ExploreSubstrings(conn, ctx, 4, substrings);
            var rand = new Random(Environment.TickCount);
            return Enumerable.Range(0, GeneratedValuesCount).Select(_
                => substrings.GenerateString(
                        minLength: 3,
                        minSubstringLength: 3,
                        maxSubstringLength: 4,
                        rand));
        }

        private static async Task<IEnumerable<string>> GenerateEmails(DConnection conn, ExplorerContext ctx)
        {
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Finds common substrings for each position in the texts of the specified column.
        /// It uses a batch approach to query for several positions (specified using SubstringQueryColumnCount)
        /// using a single query.
        /// </summary>
        private static async Task ExploreSubstrings(DConnection conn, ExplorerContext ctx, int length, SubstringDataCollection substrings)
        {
            var hasRows = true;
            for (var pos = 0; hasRows; pos += SubstringQueryColumnCount)
            {
                var sstrResult = await conn.Exec(new TextColumnSubstring(ctx.Table, ctx.Column, pos, length, SubstringQueryColumnCount));
                hasRows = false;
                foreach (var row in sstrResult.Rows)
                {
                    if (row.HasValue)
                    {
                        hasRows = true;
                        substrings.Add(pos + row.Index, row.Value, row.Count);
                    }
                }
            }
        }

        private static async Task<bool> CheckIsEmail(DConnection conn, ExplorerContext ctx)
        {
            var emailCheck = await conn.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Both, EmailAddressChars));

            var counts = ValueCounts.Compute(emailCheck.Rows);

            return counts.TotalCount == emailCheck.Rows
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);
        }

    }

    internal class SubstringWithCountList : List<(string Value, long Count)>
    {
        public long TotalCount => Count == 0 ? 0 : this[^1].Count;

        public string GetSubstring(Random rand)
        {
            if (Count == 0)
            {
                return string.Empty;
            }
            var rcount = rand.NextLong(TotalCount);
            return FindSubstring(rcount);
    }

        private string FindSubstring(long count)
        {
            var left = 0;
            var right = Count - 1;
            while (true)
            {
                var middle = (left + right) / 2;
                if (middle == 0 || middle == Count - 1)
                {
                    return this[middle].Value;
                }
                if (count < this[middle].Count)
                {
                    if (count >= this[middle - 1].Count)
                    {
                        return this[middle - 1].Value;
                    }
                    right = middle;
                }
                else if (count > this[middle].Count)
                {
                    if (count <= this[middle + 1].Count)
                    {
                        return this[middle].Value;
                    }
                    left = middle;
                }
                else
                {
                    return this[middle].Value;
                }
            }
        }
    }

    /// <summary>
    /// Stores the substrings from a certain position in a column,
    /// together with the number of occurences (counts) for each substring.
    /// The substrings are grouped separately by length.
    /// </summary>
    internal class SubstringsData
    {
        public SubstringsData(int maxSubstringLength)
        {
            Data = new List<SubstringWithCountList>(maxSubstringLength)
            {
                new SubstringWithCountList() { (string.Empty, 0) },
            };
            for (var i = 1; i <= maxSubstringLength; i++)
            {
                Data.Add(new SubstringWithCountList());
            }
        }

        private List<SubstringWithCountList> Data { get; }

        public void Add(string s, long count)
        {
            var substrings = Data[s.Length];
            substrings.Add((s, substrings.TotalCount + count));
        }

        public string GetSubstring(int minLength, int maxLength, Random rand)
        {
            if (maxLength >= Data.Count)
            {
                throw new ArgumentException($"{nameof(maxLength)} should be smaller than {Data.Count}.", nameof(maxLength));
            }
            // TODO: distribute value over all alternatives according to counts (not with the same probability)
            var sslen = rand.Next(minLength, maxLength + 1);
            var substrings = Data[sslen];
            return substrings.GetSubstring(rand);
        }
    }

    internal class SubstringDataCollection
    {
        public SubstringDataCollection(int maxSubstringLength)
        {
            MaxSubstringLength = maxSubstringLength;
            Substrings = new List<SubstringsData>();
        }

        private List<SubstringsData> Substrings { get; }

        private int MaxSubstringLength { get; }

        public void Add(int pos, string s, long count)
        {
            while (Substrings.Count <= pos)
            {
                Substrings.Add(new SubstringsData(MaxSubstringLength));
            }
            Substrings[pos].Add(s, count);
        }

        public string GenerateString(int minLength, int minSubstringLength, int maxSubstringLength, Random rand)
        {
            var sb = new StringBuilder();
            var len = rand.Next(minLength, Substrings.Count);
            for (var pos = 0; pos < Substrings.Count && sb.Length < len; pos++)
            {
                var str = Substrings[pos].GetSubstring(minSubstringLength, maxSubstringLength, rand);
                sb.Append(str);
                pos += str.Length;
            }
            return sb.ToString();
        }
    }

}
