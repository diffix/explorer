namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Common;
    using Explorer.Explorers;
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
            var metrics = await GetExplorerMetrics(new EmailColumnExplorer(), "gda_banking", "clients", "email");

            Assert.Single(metrics, m => m.Name == "is_email");
            Assert.Single(metrics, m => m.Name == "email.top_level_domains");
            Assert.Single(metrics, m => m.Name == "email.domains");

            var top_level_domains = metrics.First(m => m.Name == "email.top_level_domains").Metric as IEnumerable<dynamic>;
            Assert.Single(top_level_domains, x => x.name == ".com");
        }

        private async Task<IEnumerable<ExploreMetric>> GetExplorerMetrics(
            ExplorerBase explorer,
            string dataSourceName,
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown,
            [CallerMemberName] string vcrSessionName = "")
        {
            return await factory.GetExplorerMetrics(
                explorer,
                dataSourceName,
                tableName,
                columnName,
                columnType,
                nameof(EmailExplorerTests),
                vcrSessionName);
        }
    }
}