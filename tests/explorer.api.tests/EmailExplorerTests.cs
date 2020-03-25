namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class EmailExplorerTests : IClassFixture<TestWebAppFactory>
    {
        private readonly TestWebAppFactory factory;

        public EmailExplorerTests(TestWebAppFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async void TestEmailExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_banking", queryResolver =>
                new EmailColumnExplorer(queryResolver, "clients", "email"));

            Assert.Single(metrics, m => m.Name == "is_email");
            Assert.Single(metrics, m => m.Name == "email.top_level_domains");
            Assert.Single(metrics, m => m.Name == "email.domains");

            var top_level_domains = metrics.First(m => m.Name == "email.top_level_domains").Metric as IEnumerable<dynamic>;
            Assert.Single(top_level_domains, x => x.name == ".com");
        }

        private async Task<IEnumerable<IExploreMetric>> GetExplorerMetrics(
            string dataSourceName,
            Func<IQueryResolver, ExplorerBase> explorerFactory,
            [CallerMemberName] string vcrSessionName = "")
        {
            return await factory.GetExplorerMetrics(dataSourceName, explorerFactory, nameof(EmailExplorerTests), vcrSessionName);
        }
    }
}