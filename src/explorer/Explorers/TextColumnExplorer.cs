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

    using LongList = System.Collections.Generic.List<long>;
    using StringList = System.Collections.Generic.List<string>;
    using SubstringsDictionary = System.Collections.Generic.Dictionary<string, long>;

    internal class TextColumnExplorer : ExplorerBase
    {
        private const double SuppressedRatioThreshold = 0.1;
        private const int SubstringQueryColumnCount = 5;
        private const int GeneratedValuesCount = 10;

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
                // we compute the common prefixes only if the row is not categorical
                // await ExplorePrefixes(conn, ctx);
                var values = await GenerateValues(conn, ctx, counts);
                PublishMetric(new UntypedMetric(name: "synthetic_values", metric: values));
            }
        }

        private async Task<IEnumerable<string>> GenerateValues(DConnection conn, ExplorerContext ctx, ValueCounts counts)
        {
            var allSubstringsDict = new List<SubstringsDictionary>();

            await ExploreSubstrings(conn, ctx, 1, allSubstringsDict);
            var isEmail = CheckIsEmail(allSubstringsDict, counts);
            PublishMetric(new UntypedMetric(name: "is_email", metric: isEmail));

            // TODO emails: use query to get top level domains; generate values according to email pattern
            await ExploreSubstrings(conn, ctx, 2, allSubstringsDict);
            await ExploreSubstrings(conn, ctx, 3, allSubstringsDict);
            await ExploreSubstrings(conn, ctx, 4, allSubstringsDict);

            // convert the Dictionary to separate lists
            // one with the substrings and one with the running total for counts
            var allSubstringsList = new List<StringList>(allSubstringsDict.Count);
            var allCountsList = new List<LongList>(allSubstringsDict.Count);
            foreach (var substringsDict in allSubstringsDict)
            {
                var substringsList = new StringList(substringsDict.Count);
                var countsList = new LongList(substringsDict.Count);
                var totalSubstringsCount = 0L;
                foreach (var kv in substringsDict)
                {
                    // TODO: determine better heuristics and optimize exploration to get the useful substrings only
                    // this condition was determined empirically to work for column containing names
                    if (kv.Key.Length == 4 || kv.Key.Length == 3)
                    {
                        totalSubstringsCount += kv.Value;
                        substringsList.Add(kv.Key);
                        countsList.Add(totalSubstringsCount);
                    }
                }
                allSubstringsList.Add(substringsList);
                allCountsList.Add(countsList);
            }

            var rand = new Random(Environment.TickCount);
            return Enumerable.Range(0, GeneratedValuesCount * 3).Select(_
                => GenerateString(allSubstringsList, allCountsList, rand));
        }

        private string GenerateString(List<StringList> substrings, List<LongList> counts, Random rand)
        {
            var sb = new StringBuilder();
            var len = rand.Next(3, substrings.Count);
            for (var pos = 0; pos < substrings.Count && sb.Length < len; pos++)
            {
                var str = FindSubstring(substrings[pos], counts[pos], rand);
                sb.Append(str);
                pos += str.Length;
            }
            return sb.ToString();
        }

        private string FindSubstring(StringList substrings, LongList counts, Random rand)
        {
            if (substrings.Count == 0)
            {
                return string.Empty;
            }
            var totalSubstringsCount = counts[^1];
            var rcount = rand.NextLong(totalSubstringsCount + 1);
            var left = 0;
            var right = substrings.Count - 1;
            while (true)
            {
                var middle = (left + right) / 2;
                if (middle == 0 || middle == substrings.Count - 1)
                {
                    return substrings[middle];
                }
                if (rcount < counts[middle])
                {
                    if (rcount >= counts[middle - 1])
                    {
                        return substrings[middle - 1];
                    }
                    right = middle;
                }
                else if (rcount > counts[middle])
                {
                    if (rcount <= counts[middle + 1])
                    {
                        return substrings[middle];
                    }
                    left = middle;
                }
                else
                {
                    return substrings[middle];
                }
            }
        }

        /// <summary>
        /// Finds common substrings for each position in the texts of the specified column.
        /// It uses a batch approach to query for several positions (specified by SubstringQueryColumnCount)
        /// using a single query. For each position in the string we create a Dictionary with the substring
        /// as key and the number of occurences of the substring as value. The dictionaries are stored in the
        /// allSubstrings parameter, the index in the list corresponding with the substring position in the
        /// column value.
        /// </summary>
        private async Task ExploreSubstrings(DConnection conn, ExplorerContext ctx, int length, List<SubstringsDictionary> allSubstrings)
        {
            var hasRows = true;
            for (var pos = 0; hasRows; pos += SubstringQueryColumnCount)
            {
                for (var i = 0; i < SubstringQueryColumnCount; i++)
                {
                    if (pos + i >= allSubstrings.Count)
                    {
                        allSubstrings.Add(new SubstringsDictionary());
                    }
                }
                var sstrResult = await conn.Exec(new TextColumnSubstring(ctx.Table, ctx.Column, pos, length, SubstringQueryColumnCount));
                hasRows = false;
                foreach (var row in sstrResult.Rows)
                {
                    if (row.HasValue)
                    {
                        hasRows = true;
                        var substrings = allSubstrings[pos + row.Index];
                        substrings.TryGetValue(row.Value, out var count);
                        substrings[row.Value] = count + row.Count;
                    }
                }
            }
        }

        private bool CheckIsEmail(IList<SubstringsDictionary> allSubstrings, ValueCounts counts)
        {
            var atsCount = 0L;
            var dotsCount = 0L;
            foreach (var substrings in allSubstrings)
            {
                foreach (var sskv in substrings)
                {
                    if (sskv.Key == "@")
                    {
                        atsCount += sskv.Value;
                    }
                    else if (sskv.Key == ".")
                    {
                        dotsCount += sskv.Value;
                    }
                }
            }
            if (atsCount / (double)counts.TotalCount < 0.99)
            {
                return false;
            }
            if (dotsCount / (double)counts.TotalCount < 0.99)
            {
                return false;
            }
            return true;
        }
    }
}
