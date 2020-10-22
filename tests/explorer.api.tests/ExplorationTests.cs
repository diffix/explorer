namespace Explorer.Api.Tests
{
    using System.Linq;
    using System.Threading.Tasks;

    using VcrSharp;
    using Xunit;

    public class ExplorationTests : IClassFixture<ExplorationTestFixture>
    {
        private readonly ExplorationTestFixture fixture;

        public ExplorationTests(ExplorationTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestIntegerColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "gda_banking",
                    "loans",
                    "duration",
                    metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestRealColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "gda_banking",
                    "loans",
                    "payments",
                    metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestBooleanColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "GiveMeSomeCredit",
                    "loans",
                    "SeriousDlqin2yrs",
                    metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestTextColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "gda_banking",
                    "loans",
                    "firstname",
                    metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public void TestTimestampColumn()
        {
            // TODO: Find a table with a timestamp column
        }

        [Fact]
        public async Task TestDateColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "gda_banking",
                    "loans",
                    "birthdate",
                    metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestDatetimeColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "gda_taxi",
                    "rides",
                    "pickup_datetime",
                    metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestMultiColumn()
        {
            using var testScope = fixture.PrepareExplorationTestScope(Cassette.GenerateVcrFilename(this));

            await testScope.RunAndCheckMetrics(
                    "gda_banking",
                    "loans",
                    new[] { "firstname", "duration" },
                    metrics =>
                    {
                        Assert.True(metrics["firstname"].Any());
                        Assert.True(metrics["duration"].Any());
                        Assert.True(metrics.Count == 2);
                    });
        }
    }
}