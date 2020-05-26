namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

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
        public async void TestMinMaxRefinerComponent()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "amount");

            await scope.Test<MinMaxRefiner, MinMaxRefiner.Result>(result =>
            {
                const decimal expectedMin = 3303;
                const decimal expectedMax = 495_103;
                Assert.True(result.Min == expectedMin, $"Expected {expectedMin}, got {result.Min}");
                Assert.True(result.Max == expectedMax, $"Expected {expectedMax}, got {result.Max}");
            });
        }

        [Fact]
        public async void TestCategoricalBool()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "cov_clear",
                "survey",
                "fever");

            await scope.Test<DistinctValuesComponent, DistinctValuesComponent.Result>(result =>
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
        public async void TestCategoricalText()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "status");

            await scope.Test<DistinctValuesComponent, DistinctValuesComponent.Result>(result =>
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

        [Fact]
        public async void TestLinearDateTimeComponentWithDateTimeColumn()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_taxi",
                "rides",
                "pickup_datetime",
                DValueType.Datetime);

            await scope.Test<LinearTimeBuckets, LinearTimeBuckets.Result>(result =>
            {
                result.Rows.Single(r => r.Key == "minute");
                result.Rows.Single(r => r.Key == "hour");
            });

            // Assert.Single(metrics, m => m.Name == "dates_cyclical.second");
            // Assert.Single(metrics, m => m.Name == "dates_cyclical.minute");
        }

        [Fact]
        public async void TestLinearDateTimeComponentWithDateColumn()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_taxi",
                "rides",
                "birthdate",
                DValueType.Date);

            await scope.Test<LinearTimeBuckets, LinearTimeBuckets.Result>(result =>
            {
                result.Rows.Single(r => r.Key == "year");
                result.Rows.Single(r => r.Key == "month");
            });

            // Assert.Single(metrics, m => m.Name == "dates_cyclical.day");
            // Assert.Single(metrics, m => m.Name == "dates_cyclical.weekday");
            // Assert.Single(metrics, m => m.Name == "dates_cyclical.month");
            // Assert.Single(metrics, m => m.Name == "dates_cyclical.quarter");
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