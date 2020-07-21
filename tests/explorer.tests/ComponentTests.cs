namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components;
    using Xunit;

    public class ComponentTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public ComponentTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async Task TestMinMaxRefinerComponent()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "amount",
                new ColumnInfo(DValueType.Integer, ColumnInfo.ColumnType.Regular),
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<MinMaxRefiner, MinMaxRefiner.Result>(result =>
            {
                const decimal expectedMin = 3303;
                const decimal expectedMax = 495_103;
                Assert.True(result.Min == expectedMin, $"Expected {expectedMin}, got {result.Min}");
                Assert.True(result.Max == expectedMax, $"Expected {expectedMax}, got {result.Max}");
            });
        }

        [Fact]
        public async Task TestDistinctValuesMetricContainsRemainder()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "duration",
                new ColumnInfo(DValueType.Integer, ColumnInfo.ColumnType.Regular),
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            scope.ConfigurePublisher<DistinctValuesComponent>(c => c.NumValuesToPublish = 1);

            await scope.MetricsTest<DistinctValuesComponent>(metrics =>
            {
                Assert.Single(metrics, m => m.Name == "distinct.values");
                dynamic valuesMetric = metrics.Single(m => m.Name == "distinct.values").Metric;
                Assert.True(valuesMetric.Count == 2);
            });
        }

        [Fact]
        public async Task TestCategoricalBool()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "cov_clear",
                "survey",
                "fever",
                new ColumnInfo(DValueType.Bool, ColumnInfo.ColumnType.Regular),
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<DistinctValuesComponent, DistinctValuesComponent.Result>(result =>
            {
                var expectedValues = new List<ValueWithCount<bool>>
                {
                    ValueWithCount<bool>.ValueCount(false, 3_468),
                    ValueWithCount<bool>.ValueCount(true, 592),
                };

                CheckDistinctCategories(result, expectedValues, el => el.GetBoolean());
            });
        }

        [Fact]
        public async Task TestCategoricalText()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "status",
                new ColumnInfo(DValueType.Text, ColumnInfo.ColumnType.Regular),
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<DistinctValuesComponent, DistinctValuesComponent.Result>(result =>
            {
                var expectedValues = new List<ValueWithCount<string>>
                {
                    ValueWithCount<string>.ValueCount("C", 491L),
                    ValueWithCount<string>.ValueCount("A", 258L),
                    ValueWithCount<string>.ValueCount("D", 45L),
                    ValueWithCount<string>.ValueCount("B", 30L),
                };

                CheckDistinctCategories(result, expectedValues, el => el.GetString());
            });
        }

        // [Theory]
        // [InlineData("birth_number", new ColumnInfo(DValueType.Text, ))]
        // [InlineData("district_id", false)]
        // public async void TestIsolatorComponent(string column, bool isIsolator)
        // {
        //     using var scope = testFixture.SimpleComponentTestScope(
        //         "gda_banking",
        //         "clients",
        //         column,
        //         vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

        //     await scope.ResultTest<IsolatorCheckComponent, IsolatorCheckComponent.Result>(result =>
        //     {
        //         Assert.Equal(result.ColumnName, column);
        //         Assert.Equal(result.IsIsolatorColumn, isIsolator);
        //     });
        // }

        private void CheckDistinctCategories<T>(
            DistinctValuesComponent.Result distinctValuesResult,
            IEnumerable<ValueWithCount<T>> expectedValues,
            Func<JsonElement, T> parseElement)
        {
            var distinctValues = distinctValuesResult.DistinctRows;

            foreach (var (actual, expected) in distinctValues.Zip(expectedValues))
            {
                // Use dynamic here to make things simpler... to do this we assume that the dynamically
                // resolved type (should be the type `T`) supports the equality operator.
                dynamic actualVal = parseElement(actual.Value)!;
                Assert.IsType<T>(actualVal);

                Assert.True(actualVal == expected.Value, $"Expected {expected}, got {actual}.");
                Assert.True(actual.Count == expected.Count, $"Expected {expected}, got {actual}.");
            }

            var expectedTotal = expectedValues.Sum(v => v.Count);
            var actualTotal = distinctValuesResult.ValueCounts.TotalCount;
            Assert.True(expectedTotal == actualTotal, $"Expected total of {expectedTotal}, got {actualTotal}");

            const long expectedSuppressed = 0L;
            var actualSuppressed = distinctValuesResult.ValueCounts.SuppressedCount;
            Assert.True(
                actualSuppressed == expectedSuppressed,
                $"Expected total of {expectedSuppressed}, got {actualSuppressed}");
        }
    }
}