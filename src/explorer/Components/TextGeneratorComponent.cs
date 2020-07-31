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

    public class TextGeneratorComponent : ExplorerComponent<TextGeneratorComponent.Result>, PublisherComponent
    {
        public const int DefaultSamplesToPublish = 20;
        public const int DefaultEmailDomainsCountThreshold = 5 * DefaultSamplesToPublish;
        public const int DefaultSubstringQueryColumnCount = 5;

        private readonly EmailCheckComponent emailChecker;
        private readonly DistinctValuesComponent distinctValues;

        public TextGeneratorComponent(
            EmailCheckComponent emailChecker,
            DistinctValuesComponent distinctValues)
        {
            this.emailChecker = emailChecker;
            this.distinctValues = distinctValues;
            SamplesToPublish = DefaultSamplesToPublish;
            EmailDomainsCountThreshold = DefaultEmailDomainsCountThreshold;
            SubstringQueryColumnCount = DefaultSubstringQueryColumnCount;
        }

        public int SamplesToPublish { get; set; }

        public int EmailDomainsCountThreshold { get; set; }

        public int SubstringQueryColumnCount { get; set; }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distinctValuesResult = await distinctValues.ResultAsync;
            if (!distinctValuesResult.IsCategorical)
            {
                var result = await ResultAsync;

                if (result.SampleValues.Count > 0)
                {
                    yield return new UntypedMetric(name: "sample_values", metric: result.SampleValues);
                }
            }
        }

        protected override async Task<Result> Explore()
        {
            var emailCheckerResult = await emailChecker.ResultAsync;
            var sampleValues = emailCheckerResult.IsEmail ? await GenerateEmails() : await GenerateStrings();
            return new Result(sampleValues.ToList());
        }

        private async Task<SubstringWithCountList> ExploreEmailDomains()
        {
            var domains = await Context.Exec(new TextColumnTrim(TextColumnTrimType.Leading, Constants.EmailAddressChars));

            return SubstringWithCountList.FromValueWithCountEnum(
                domains.Rows
                    .Where(r => r.HasValue && r.Value.StartsWith("@", StringComparison.InvariantCulture)));
        }

        private async Task<SubstringWithCountList> ExploreEmailTopLevelDomains()
        {
            var suffixes = await Context.Exec(new TextColumnSuffix(3, 7));

            return SubstringWithCountList.FromValueWithCountEnum(
                suffixes.Rows
                    .Where(r => r.HasValue && r.Value.StartsWith(".", StringComparison.InvariantCulture)));
        }

        private static string GenerateString(SubstringsData substrings, int minLength, Random rand)
        {
            var sb = new StringBuilder();
            var len = rand.Next(minLength, minLength + substrings.Count);
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
                return localPart + domains.GetRandomValue(rand);
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
            return localPart + "@" + domain + tlds.GetRandomValue(rand);
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
        private async Task<SubstringsData> ExploreSubstrings(
            int substringQueryColumnCount,
            params int[] substringLengths)
        {
            var substrings = new SubstringsData();
            foreach (var length in substringLengths)
            {
                var hasRows = true;
                for (var pos = 0; hasRows; pos += substringQueryColumnCount)
                {
                    var query = new TextColumnSubstring(pos, length, substringQueryColumnCount);
                    var sstrResult = await Context.Exec(query);
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

        private async Task<IEnumerable<string>> GenerateEmails()
        {
            var (substrings, domains, tlds) = await Utilities.WhenAll(
                ExploreSubstrings(SubstringQueryColumnCount, substringLengths: new int[] { 3, 4 }),
                ExploreEmailDomains(),
                ExploreEmailTopLevelDomains());
            if (substrings.Count == 0 || tlds.Count == 0)
            {
                return Enumerable.Empty<string>();
            }
            return GenerateEmails(substrings, domains, tlds, SamplesToPublish, EmailDomainsCountThreshold);
        }

        private async Task<IEnumerable<string>> GenerateStrings()
        {
            // the substring lengths 3 and 4 were determined empirically to work for column containing names
            var substrings = await ExploreSubstrings(
                SubstringQueryColumnCount, substringLengths: new int[] { 3, 4 });
            if (substrings.Count == 0)
            {
                return Enumerable.Empty<string>();
            }
            var rand = new Random(Environment.TickCount);
            return Enumerable.Range(0, SamplesToPublish).Select(_
                => GenerateString(substrings, minLength: 3, rand));
        }

        public class Result
        {
            public Result(IList<string> sampleValues)
            {
                SampleValues = sampleValues;
            }

            public IList<string> SampleValues { get; }
        }
    }
}