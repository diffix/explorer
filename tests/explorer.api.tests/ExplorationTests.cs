namespace Explorer.Api.Tests
{
    using System.Linq;
    using System.Threading.Tasks;

    using VcrSharp;
    using Xunit;

    public class ExplorationTests : IClassFixture<ExplorationTestFixture>
    {
        private readonly ExplorationTestScope testScope;

        public ExplorationTests(ExplorationTestFixture fixture)
        {
            testScope = fixture
                .PrepareExplorationTestScope()
                .LoadCassette(Cassette.GenerateVcrFilename(this, nameof(TestIntegerColumn)));
        }

        [Fact]
        public async Task TestIntegerColumn()
        {
            await testScope.RunAndCheckMetrics(
                "gda_banking",
                "loans",
                "duration",
                metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestRealColumn()
        {
            await testScope.RunAndCheckMetrics(
                "gda_banking",
                "loans",
                "payments",
                metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestBooleanColumn()
        {
            await testScope.RunAndCheckMetrics(
                "GiveMeSomeCreditOnline",
                "loans",
                "SeriousDlqin2yrs",
                metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestTextColumn()
        {
            await testScope.RunAndCheckMetrics(
                "gda_banking",
                "loans",
                "firstname",
                metrics => Assert.True(metrics.Any()));
        }

        // TODO: Find a table with a timestamp column
        // [Fact]
        // public async Task TestTimestampColumn()
        // {   
        // }

        [Fact]
        public async Task TestDateColumn()
        {
            await testScope.RunAndCheckMetrics(
                "gda_banking",
                "loans",
                "birthdate",
                metrics => Assert.True(metrics.Any()));
        }

        [Fact]
        public async Task TestDatetimeColumn()
        {
            await testScope.RunAndCheckMetrics(
                "gda_taxi",
                "rides",
                "pickup_datetime",
                metrics => Assert.True(metrics.Any()));
        }
    }
}