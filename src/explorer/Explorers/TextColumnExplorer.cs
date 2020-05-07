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

    using SubstringWithCountList = Explorer.Common.ValueWithCountList<string>;

    internal class TextColumnExplorer : ExplorerBase
    {
        public const string EmailAddressChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";
        private const double SuppressedRatioThreshold = 0.1;
        private const int SubstringQueryColumnCount = 5;
        private const int GeneratedValuesCount = 30;
        private const int EmailDomainsCountThreshold = 5 * GeneratedValuesCount;

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
            var substrings = await ExploreSubstrings(conn, ctx, substringLengths: new int[] { 3, 4 });
            var rand = new Random(Environment.TickCount);
            return Enumerable.Range(0, GeneratedValuesCount).Select(_
                => GenerateString(substrings, minLength: 3, rand));
        }

        private static async Task<IEnumerable<string>> GenerateEmails(DConnection conn, ExplorerContext ctx)
        {
            var domains = await ExploreEmailDomains(conn, ctx);
            var tlds = await ExploreEmailTopLevelDomains(conn, ctx);
            var substrings = await ExploreSubstrings(conn, ctx, substringLengths: new int[] { 3, 4 });
            var rand = new Random(Environment.TickCount);
            var emails = new List<string>(GeneratedValuesCount);
            for (var i = 0; emails.Count < GeneratedValuesCount && i < GeneratedValuesCount * 100; i++)
            {
                var email = GenerateEmail(substrings, domains, tlds, rand);
                if (!string.IsNullOrEmpty(email))
                {
                    emails.Add(email);
                }
            }
            return emails;
        }

        private static string GenerateString(SubstringsData substrings, int minLength, Random rand)
        {
            var sb = new StringBuilder();
            var len = rand.Next(minLength, substrings.Count);
            for (var pos = 0; pos < substrings.Count && sb.Length < len; pos++)
            {
                var str = substrings.GetRandomSubstring(pos, rand);
                sb.Append(str);
                pos += str.Length;
            }
            return sb.ToString();
        }

        private static string GenerateEmail(
            SubstringsData substrings,
            SubstringWithCountList domains,
            SubstringWithCountList tlds,
            Random rand)
        {
            // create local-part section
            var str = GenerateString(substrings, minLength: 6, rand);
            var allParts = str.Split('@', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            var partIndex = 0;
            var pnext = 1;
            while (partIndex < allParts.Length && rand.NextDouble() <= pnext)
            {
                sb.Append(allParts[partIndex]);
                pnext /= 2;
                partIndex++;
            }
            for (var replaced = 1; replaced != 0;)
            {
                var oldlen = sb.Length;
                sb.Replace("..", ".");
                replaced = oldlen - sb.Length;
            }
            var localParts = sb.ToString()
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length == 1 || s.Length > 3)
                .Take(rand.Next(1, 3));
            var localPart = string.Join('.', localParts);
            if (string.IsNullOrEmpty(localPart))
            {
                return string.Empty;
            }
            if (domains.Count >= EmailDomainsCountThreshold)
            {
                // if the number of distinct domains is big enough we select one from the extracted list
                return localPart + domains.GetRandomValue(rand, @default: string.Empty);
            }

            // create domain section
            sb.Clear();
            while (partIndex < allParts.Length)
            {
                sb.Append(allParts[partIndex]);
                partIndex++;
            }
            var domainParts = sb.ToString()
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 3);
            var domain = rand.NextDouble() > 0.15 ?
                domainParts.Aggregate(string.Empty, (max, cur) => max.Length > cur.Length ? max : cur) :
                string.Join('.', domainParts);
            if (string.IsNullOrEmpty(domain) || domain.Length < 4)
            {
                return string.Empty;
            }
            return localPart + "@" + domain + tlds.GetRandomValue(rand, @default: string.Empty);
        }

        /// <summary>
        /// Finds common substrings for each position in the texts of the specified column.
        /// It uses a batch approach to query for several positions (specified using SubstringQueryColumnCount)
        /// using a single query.
        /// </summary>
        private static async Task<SubstringsData> ExploreSubstrings(DConnection conn, ExplorerContext ctx, params int[] substringLengths)
        {
            var substrings = new SubstringsData();
            foreach (var length in substringLengths)
            {
                var hasRows = true;
                for (var pos = 0; hasRows; pos += SubstringQueryColumnCount)
                {
                    var query = new TextColumnSubstring(ctx.Table, ctx.Column, pos, length, SubstringQueryColumnCount);
                    var sstrResult = await conn.Exec(query);
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
            return substrings;
        }

        private static async Task<bool> CheckIsEmail(DConnection conn, ExplorerContext ctx)
        {
            var emailCheck = await conn.Exec(
                new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Both, EmailAddressChars));

            return emailCheck.Rows.All(r => r.IsNull || r.Value == "@");
        }

        private static async Task<SubstringWithCountList> ExploreEmailDomains(DConnection conn, ExplorerContext ctx)
        {
            var domains = await conn.Exec(new TextColumnTrim(ctx.Table, ctx.Column, TextColumnTrimType.Leading, EmailAddressChars));

            return SubstringWithCountList.FromValueWithCountEnum(
                domains.Rows
                    .Where(r => r.HasValue && r.Value.StartsWith("@", StringComparison.InvariantCulture)));
        }

        private static async Task<SubstringWithCountList> ExploreEmailTopLevelDomains(DConnection conn, ExplorerContext ctx)
        {
            var suffixes = await conn.Exec(new TextColumnSuffix(ctx.Table, ctx.Column, 3, 7));

            return SubstringWithCountList.FromValueWithCountEnum(
                suffixes.Rows
                    .Where(r => r.HasValue && r.Value.StartsWith(".", StringComparison.InvariantCulture)));
        }

        /// <summary>
        /// Stores the substrings at each position in a column,
        /// together with the number of occurences (counts) for each substring.
        /// </summary>
        internal class SubstringsData
        {
            public SubstringsData()
            {
                Substrings = new List<SubstringWithCountList>();
            }

            public int Count => Substrings.Count;

            private List<SubstringWithCountList> Substrings { get; }

            public void Add(int pos, string s, long count)
            {
                while (Substrings.Count <= pos)
                {
                    Substrings.Add(new SubstringWithCountList());
                }
                Substrings[pos].AddValueCount(s, count);
            }

            public string GetRandomSubstring(int pos, Random rand)
            {
                return Substrings[pos].GetRandomValue(rand, string.Empty);
            }

            internal class Item
            {
                public Item(int maxSubstringLength)
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
                    Data[s.Length].AddValueCount(s, count);
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
                    return substrings.GetRandomValue(rand, string.Empty);
                }
            }
        }
