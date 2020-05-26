namespace Explorer.Tests
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

    public sealed class TextColumnExplorerTests : IClassFixture<ExplorerTestFixture>
    {
        public TextColumnExplorerTests()
        {
        }

        // [Fact]
        // public async void TestClientsEmail()
        // {
        //     var metrics = await GetExplorerMetrics(new TextColumnExplorer(), "gda_banking", "clients", "email");
        //     var isEmail = metrics.First(m => m.Name == "is_email");
        //     Assert.True((bool)isEmail.Metric);
        //     var metric_svalues = metrics.First(m => m.Name == "synthetic_values");
        //     var values = metric_svalues.Metric as IEnumerable<string>;
        //     Assert.True(values.All(v => v.Length >= 3));
        //     Assert.True(values.All(v => v.Count(c => c == '@') == 1));
        //     Assert.True(values.All(v => v.Contains('.', StringComparison.InvariantCulture)));
        //     Assert.True(values.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
        // }

        // [Fact]
        // public async void TestCardsEmail()
        // {
        //     var metrics = await GetExplorerMetrics(new TextColumnExplorer(), "gda_banking", "cards", "email");
        //     var isEmail = metrics.First(m => m.Name == "is_email");
        //     Assert.True((bool)isEmail.Metric);
        //     var metric_svalues = metrics.First(m => m.Name == "synthetic_values");
        //     var values = metric_svalues.Metric as IEnumerable<string>;
        //     Assert.True(values.All(v => v.Length >= 3));
        //     Assert.True(values.All(v => v.Count(c => c == '@') == 1));
        //     Assert.True(values.All(v => v.Contains('.', StringComparison.InvariantCulture)));
        //     Assert.True(values.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
        // }

        // [Fact]
        // public async void TestFirstName()
        // {
        //     var metrics = await GetExplorerMetrics(new TextColumnExplorer(), "gda_banking", "cards", "firstname");
        //     var isEmail = metrics.First(m => m.Name == "is_email");
        //     Assert.False((bool)isEmail.Metric);
        //     var metric_svalues = metrics.First(m => m.Name == "synthetic_values");
        //     var values = metric_svalues.Metric as IEnumerable<string>;
        //     Assert.True(values.All(v => v.Length >= 3));
        // }

        // [Fact]
        // public async void TestLastName()
        // {
        //     var metrics = await GetExplorerMetrics(new TextColumnExplorer(), "gda_banking", "cards", "lastname");
        //     var isEmail = metrics.First(m => m.Name == "is_email");
        //     Assert.False((bool)isEmail.Metric);
        //     var metric_svalues = metrics.First(m => m.Name == "synthetic_values");
        //     var values = metric_svalues.Metric as IEnumerable<string>;
        //     Assert.True(values.All(v => v.Length >= 3));
        // }

        // private async Task<IEnumerable<ExploreMetric>> GetExplorerMetrics(
        //     ExplorerBase explorer,
        //     string dataSourceName,
        //     string tableName,
        //     string columnName,
        //     DValueType columnType = DValueType.Unknown,
        //     [CallerMemberName] string vcrSessionName = "")
        // {
        //     return await factory.GetExplorerMetrics(
        //         explorer,
        //         dataSourceName,
        //         tableName,
        //         columnName,
        //         columnType,
        //         "~" + nameof(TextColumnExplorerTests),
        //         vcrSessionName);
        // }
    }
}