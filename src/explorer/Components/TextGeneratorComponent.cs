namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    using SubstringWithCountList = Explorer.Common.ValueWithCountList<string>;

    public class TextGeneratorComponent : ExplorerComponent<IEnumerable<string>>, PublisherComponent
    {
        private const int DefaultGeneratedValuesCount = 30;
        private const int DefaultEmailDomainsCountThreshold = 5 * DefaultGeneratedValuesCount;
        private const int DefaultSubstringQueryColumnCount = 5;

        private readonly DConnection conn;
        private readonly ExplorerContext ctx;
        private readonly EmailCheckComponent emailChecker;

        public TextGeneratorComponent(DConnection conn, ExplorerContext ctx, EmailCheckComponent emailChecker)
        {
            this.conn = conn;
            this.ctx = ctx;
            this.emailChecker = emailChecker;
            GeneratedValuesCount = DefaultGeneratedValuesCount;
            EmailDomainsCountThreshold = DefaultEmailDomainsCountThreshold;
            SubstringQueryColumnCount = DefaultSubstringQueryColumnCount;
        }

        public int GeneratedValuesCount { get; set; }

        public int EmailDomainsCountThreshold { get; set; }

        public int SubstringQueryColumnCount { get; set; }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;

            if (result.Any())
            {
                yield return new UntypedMetric(name: "sample_values", metric: result.ToArray());
            }
        }

        protected override async Task<IEnumerable<string>> Explore()
        {
            var isEmail = await emailChecker.ResultAsync;

            return isEmail
                ? await GenerateEmails()
                : await GenerateStrings();
        }

        private static async Task<SubstringWithCountList> ExploreEmailDomains(DConnection conn, ExplorerContext ctx)
        {
            var domains = await conn.Exec(new TextColumnTrim(
                ctx.Table, ctx.Column, TextColumnTrimType.Leading, Constants.EmailAddressChars));

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

        private async Task<IEnumerable<string>> GenerateEmails()
        {
            var (substrings, domains, tlds) = await Utilities.WhenAll(
                ExploreSubstrings(
                    conn, ctx, SubstringQueryColumnCount, substringLengths: new int[] { 3, 4 }),
                ExploreEmailDomains(conn, ctx),
                ExploreEmailTopLevelDomains(conn, ctx));

            return await Task.Run(() => GenerateEmails(
                    substrings, domains, tlds, GeneratedValuesCount, EmailDomainsCountThreshold));
        }

        private async Task<IEnumerable<string>> GenerateStrings()
        {
            // the substring lengths 3 and 4 were determined empirically to work for column containing names
            var substrings = await ExploreSubstrings(
                conn, ctx, SubstringQueryColumnCount, substringLengths: new int[] { 3, 4 });
            var rand = new Random(Environment.TickCount);
            return Enumerable.Range(0, GeneratedValuesCount).Select(_
                => GenerateString(substrings, minLength: 3, rand));
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
            Random rand,
            int domainsCountThreshold)
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
            var localParts = sb.ToString()
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length == 1 || s.Length > 3)
                .Take(rand.Next(1, 3));
            var localPart = string.Join('.', localParts);
            if (string.IsNullOrEmpty(localPart))
            {
                return string.Empty;
            }
            if (domains.Count >= domainsCountThreshold)
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

        private static IEnumerable<string> GenerateEmails(
            SubstringsData substrings,
            SubstringWithCountList domains,
            SubstringWithCountList tlds,
            int generatedValuesCount,
            int domainsCountThreshold)
        {
            var rand = new Random(Environment.TickCount);
            var emails = new List<string>(generatedValuesCount);
            for (var i = 0; emails.Count < generatedValuesCount && i < generatedValuesCount * 100; i++)
            {
                var email = GenerateEmail(substrings, domains, tlds, rand, domainsCountThreshold);
                if (!string.IsNullOrEmpty(email))
                {
                    emails.Add(email);
                }
            }
            return emails;
        }

        /// <summary>
        /// Finds common substrings for each position in the texts of the specified column.
        /// It uses a batch approach to query for several positions (specified using SubstringQueryColumnCount)
        /// using a single query.
        /// </summary>
        private static async Task<SubstringsData> ExploreSubstrings(
            DConnection conn,
            ExplorerContext ctx,
            int substringQueryColumnCount,
            params int[] substringLengths)
        {
            var substrings = new SubstringsData();
            foreach (var length in substringLengths)
            {
                var hasRows = true;
                for (var pos = 0; hasRows; pos += substringQueryColumnCount)
                {
                    var query = new TextColumnSubstring(ctx.Table, ctx.Column, pos, length, substringQueryColumnCount);
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
    }
}