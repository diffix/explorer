namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Common.Utils;
    using Explorer.Components;
    using Explorer.Queries;
    using Xunit;

    public class ComponentTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public ComponentTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        public enum MinMaxRefinerTest
        {
            /// <summary>Test the refined min and max.</summary>
            Both,

            /// <summary>Test the refined min only.</summary>
            MinOnly,

            /// <summary>Test the refined max only.</summary>
            MaxOnly,
        }

        [Theory]
        [InlineData(MinMaxRefinerTest.Both, "gda_banking", "transactions", "amount")]
        [InlineData(MinMaxRefinerTest.MaxOnly, "taxi", "jan08", "pickup_longitude")]
        [InlineData(MinMaxRefinerTest.MinOnly, "scihub", "sep2015", "long")]
        public async Task TestMinMaxRefinerComponent(MinMaxRefinerTest test, string dataSource, string table, string column)
        {
            using var scope = await testFixture.CreateTestScope(dataSource, table, column, this);

            // Construct MinMaxRefiner explicitly in order to inject a null result from MinMaxFromHistogramComponent
            var simpleStatsProvider = new SimpleStats<decimal>() { Context = scope.Context };
            var histogramMinMaxProvider = new StaticResultProvider<MinMaxFromHistogramComponent.Result>(null!);
            var refiner = new MinMaxRefiner(simpleStatsProvider, histogramMinMaxProvider) { Context = scope.Context };
            var refined = await refiner.ResultAsync;

            if (test != MinMaxRefinerTest.MaxOnly)
            {
                var aircloakMin = (await scope.QueryRows<Min, Min.Result<decimal>>(new Min())).Single().Min;
                Assert.True(refined?.Min < aircloakMin, $"Expected lower than {aircloakMin}, got {refined?.Min}");
            }
            if (test != MinMaxRefinerTest.MinOnly)
            {
                var aircloakMax = (await scope.QueryRows<Max, Max.Result<decimal>>(new Max())).Single().Max;
                Assert.True(refined?.Max > aircloakMax, $"Expected higher than {aircloakMax}, got {refined?.Max}");
            }
        }

        [Fact]
        public async Task TestDistinctValuesMetricContainsRemainder()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "loans", "duration", this);

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
            using var scope = await testFixture.CreateTestScope("cov_clear", "survey", "fever", this);

            await scope.ResultTest<DistinctValuesComponent, DistinctValuesComponent.Result>(result =>
            {
                var expectedValues = new List<ValueWithCount<bool>>
                {
                    ValueWithCount<bool>.ValueCount(false, 3_468),
                    ValueWithCount<bool>.ValueCount(true, 592),
                };

                Assert.NotNull(result);
                CheckDistinctCategories(result!, expectedValues, el => el.GetBoolean());
            });
        }

        [Fact]
        public async Task TestCategoricalText()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "loans", "status", this);

            await scope.ResultTest<DistinctValuesComponent, DistinctValuesComponent.Result>(result =>
            {
                var expectedValues = new List<ValueWithCount<string>>
                {
                    ValueWithCount<string>.ValueCount("C", 491L),
                    ValueWithCount<string>.ValueCount("A", 258L),
                    ValueWithCount<string>.ValueCount("D", 45L),
                    ValueWithCount<string>.ValueCount("B", 30L),
                };

                Assert.NotNull(result);
                CheckDistinctCategories(result!, expectedValues, el => el.GetString());
            });
        }

        [Fact]
        public async Task TestHistogramMinMaxComponent()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "loans", "duration", this);

            await scope.ResultTest<MinMaxFromHistogramComponent, MinMaxFromHistogramComponent.Result>(result =>
            {
                const decimal expectedMin = 12.0M;
                const decimal expectedMax = 61.0M;
                Assert.True(result?.Min == expectedMin, $"Expected {expectedMin}, got {result?.Min}");
                Assert.True(result?.Max == expectedMax, $"Expected {expectedMax}, got {result?.Max}");
            });
        }

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
                var actualVal = parseElement(actual.Value);
                dynamic dynActualVal = actualVal!;
                Assert.IsType<T>(dynActualVal);

                Assert.True(dynActualVal == expected.Value, $"Expected {expected}, got {actual}.");
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

        public class StaticResultProvider<T> : ResultProvider<T>
        where T : class
        {
            public StaticResultProvider(T? result)
            {
                ResultAsync = Task.FromResult(result);
            }

            public Task<T?> ResultAsync { get; }
        }
    }
}