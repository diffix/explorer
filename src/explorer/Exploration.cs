namespace Explorer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Metrics;

    public sealed class Exploration : AbstractExploration, IDisposable
    {
        private bool disposedValue;

        public Exploration(string dataSource, string table, IEnumerable<ExplorationScope> scopes)
        {
            DataSource = dataSource;
            Table = table;
            ColumnExplorations = scopes.Select(scope => new ColumnExploration(scope)).ToList();
        }

        public string DataSource { get; }

        public string Table { get; }

        public List<ColumnExploration> ColumnExplorations { get; }

        public IEnumerable<IEnumerable<object?>> SampleData
        {
            get
            {
                if (!Completion.IsCompletedSuccessfully)
                {
                    yield break;
                }
                var valuesList = ColumnExplorations
                    .Select(ce => ce.PublishedMetrics.SingleOrDefault(m => m.Name == "sample_values")?.Metric as IEnumerable)
                    .Select(metric => metric?.Cast<object?>());
                var numSamples = valuesList.DefaultIfEmpty().Max(col => col?.Count() ?? 0);
                for (var i = 0; i < numSamples; i++)
                {
                    yield return valuesList.Select(sampleColumn => sampleColumn?.ElementAtOrDefault(i));
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override Task RunTask() => Task.WhenAll(ColumnExplorations.Select(ce => ce.Completion));

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var item in ColumnExplorations)
                    {
                        item.Dispose();
                    }
                }
                disposedValue = true;
            }
        }
    }
}
