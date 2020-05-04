namespace Explorer.Explorers
{
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;

    internal class TextLengthExplorer : IntegerColumnExplorer
    {
        private readonly string metricNamePrefix;

        public TextLengthExplorer(string metricNamePrefix = "text.length")
        {
            this.metricNamePrefix = metricNamePrefix;
        }

        public override async Task Explore(DConnection conn, ExplorerContext ctx)
        {
            var isolators = await conn.Exec(new IsIsolatorColumn(ctx.Table));
            var isIsolatorColumn = isolators.Rows.First(r => r.Item1 == ctx.Column).Item2;

            if (isIsolatorColumn)
            {
                // TODO: Log the fact that we are aborting here.
                return;
            }

            var lengthContext = new ExplorerContext(ctx.Table, $"length({ctx.Column})", ctx.ColumnType);

            await base.Explore(conn, lengthContext);
        }

        public override void PublishMetric(ExploreMetric metric)
        {
            if (!string.IsNullOrEmpty(metricNamePrefix))
            {
                metric = new UntypedMetric(metricNamePrefix + "." + metric.Name, metric.Metric);
            }
            base.PublishMetric(metric);
        }

        // TODO: replace this with a component
        private class IsIsolatorColumn : DQuery<(string, bool)>
        {
            public IsIsolatorColumn(string table)
            {
                QueryStatement = $"show columns from {table}";
            }

            public string QueryStatement { get; }

            public (string, bool) ParseRow(ref Utf8JsonReader reader)
            {
                reader.Read();
                var name = reader.GetString();

                reader.Read(); // ignore data type

                reader.Read();
                var isolator = reader.GetString() == "true";

                reader.Read(); // ignore key type

                return (name, isolator);
            }
        }
    }
}
