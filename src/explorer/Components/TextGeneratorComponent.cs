namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                yield return new UntypedMetric(name: "synthetic_values", metric: result);
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
                ctx.Table, ctx.Column, TextColumnTrimType.Leading, TextUtilities.EmailAddressChars));

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
                TextUtilities.ExploreSubstrings(
                    conn, ctx, SubstringQueryColumnCount, substringLengths: new int[] { 3, 4 }),
                ExploreEmailDomains(conn, ctx),
                ExploreEmailTopLevelDomains(conn, ctx));

            return await Task.Run(() => TextUtilities.GenerateEmails(
                    substrings, domains, tlds, GeneratedValuesCount, EmailDomainsCountThreshold));
        }

        private async Task<IEnumerable<string>> GenerateStrings()
        {
            // the substring lengths 3 and 4 were determined empirically to work for column containing names
            var substrings = await TextUtilities.ExploreSubstrings(
                conn, ctx, SubstringQueryColumnCount, substringLengths: new int[] { 3, 4 });
            var rand = new Random(Environment.TickCount);
            return Enumerable.Range(0, GeneratedValuesCount).Select(_
                => TextUtilities.GenerateString(substrings, minLength: 3, rand));
        }
    }
}