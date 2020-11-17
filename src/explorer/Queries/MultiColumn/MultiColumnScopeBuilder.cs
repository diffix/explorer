namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components;
    using Explorer.Metrics;
    using Explorer.Queries;

    public sealed class MultiColumnScopeBuilder : ExplorationScopeBuilder
    {
        public MultiColumnScopeBuilder(IEnumerable<MetricsPublisher> singleColumnPublishers)
        {
            SingleColumnPublishers = singleColumnPublishers;
        }

        public IEnumerable<MetricsPublisher> SingleColumnPublishers { get; }

        protected override void Configure(ExplorationScope scope, ExplorerContext context)
        {
            var metadata = context.Columns
                .Zip2(context.ColumnInfos, SingleColumnPublishers)
                .Select((_, i) => new SingleColumnMetadata(_.Item1, i, _.Item2, _.Item3));

            scope.AddPublisher<ColumnCorrelationComponent>(
                initialise: c => c.Projections = BuildProjections(metadata).ToImmutableArray());

            scope.AddPublisher<CorrelatedSampleGenerator>();
        }

        private static IEnumerable<ColumnProjection> BuildProjections(IEnumerable<SingleColumnMetadata> metadata)
            => metadata.Select(GetProjection).Where(p => !(p is IgnoredColumnProjection));

        private static ColumnProjection GetProjection(SingleColumnMetadata metadata)
        {
            var columnType = metadata.ColumnInfo.Type;

            if (metadata.IsCategoricalColumn)
            {
                if (metadata.IsInvariantColumn)
                {
                    return new IgnoredColumnProjection(metadata.Column, metadata.Index);
                }

                return new IdentityProjection(metadata.Column, metadata.Index, metadata.ColumnInfo.Type);
            }

            return columnType switch
            {
                DValueType.Date => DateTimeProjection(metadata),
                DValueType.Datetime => DateTimeProjection(metadata),
                DValueType.Timestamp => DateTimeProjection(metadata),
                DValueType.Bool => new IdentityProjection(metadata.Column, metadata.Index, columnType),
                DValueType.Integer => NumericProjection(metadata, columnType),
                DValueType.Real => NumericProjection(metadata, columnType),
                DValueType.Text => TextProjection(metadata),
                _ => new IgnoredColumnProjection(metadata.Column, metadata.Index),
            };

            static ColumnProjection NumericProjection(SingleColumnMetadata metadata, DValueType columnType)
            {
                var distribution = metadata.TryGetMetric<NumericDistribution>("descriptive_stats");
                var decimalsCountDistribution = (columnType != DValueType.Real) ?
                        null : metadata.TryGetMetric<NumericDistribution>("distinct.decimals_count_distribution");

                return new BucketisingProjection(
                    metadata.Column, metadata.ColumnInfo.Type, metadata.Index, distribution, decimalsCountDistribution);
            }

            static ColumnProjection TextProjection(SingleColumnMetadata metadata)
            {
                // TODO: somehow use string patterns.
                return new IgnoredColumnProjection(metadata.Column, metadata.Index);
            }

            static ColumnProjection DateTimeProjection(SingleColumnMetadata metadata)
            {
                // Select an appropriate bucket for time values. From the metrics we can get lists of buckets at
                // various time intervals (year, quarter, month, day, etc.).
                var datetimeMetrics = metadata
                    .TryGetMetrics<dynamic>("dates_linear.")

                    // Filter out any results with more than 10% suppressed values.
                    .Where(m => ((double)m.Metric.Suppressed / m.Metric.Total) < 0.1)

                    // Only keep an interval if the buckets it contains are not too noisy.
                    .Where(m =>
                    {
                        var noiseSum = 0.0;
                        var count = 0;
                        if (m.Metric.Counts is IEnumerable<dynamic> list)
                        {
                            count = list.Count();
                            noiseSum = list.Sum(_ =>
                            {
                                double noise = Convert.ToDouble(_.CountNoise ?? 0);
                                return noise * noise;
                            });
                        }

                        var countsAvg = (double)m.Metric.Total / count;
                        var noiseAvg = Math.Sqrt((double)noiseSum / count);

                        var threshold = noiseAvg * 5;

                        return countsAvg > threshold;
                    });

                if (!datetimeMetrics.Any())
                {
                    return new IgnoredColumnProjection(metadata.Column, metadata.Index);
                }

                var dateInterval = datetimeMetrics.OrderByDescending(m =>
                {
                    var count = 0;
                    if (m.Metric.Counts is IEnumerable<dynamic> list)
                    {
                        count = list.Count();
                    }
                    return count;
                })
                .Select(_ => _.Name.Split('.', 2)[^1])
                .First();

                return new DatetimeProjection(metadata.Column, metadata.Index, dateInterval);
            }
        }

        private class SingleColumnMetadata
        {
            private readonly MetricsPublisher metricsPublisher;

            public SingleColumnMetadata(
                string column,
                int index,
                DColumnInfo columnInfo,
                MetricsPublisher metricsPublisher)
            {
                this.metricsPublisher = metricsPublisher;

                Column = column;
                Index = index;
                ColumnInfo = columnInfo;
            }

            public string Column { get; }

            public int Index { get; }

            public DColumnInfo ColumnInfo { get; }

            public IEnumerable<ExploreMetric> Metrics => metricsPublisher.PublishedMetrics;

            public bool IsCategoricalColumn => TryGetMetric<bool>("distinct.is_categorical");

            public bool IsInvariantColumn
            {
                get
                {
                    if (!IsCategoricalColumn)
                    {
                        return false;
                    }

                    return TryGetMetric<IEnumerable<object>>("distinct.values").Count() <= 1;
                }
            }

            public T TryGetMetric<T>(string metricName)
            {
                try
                {
                    var value = Metrics.Single(metric => metric.Name == metricName).Metric;

                    if (value is T typedValue)
                    {
                        return typedValue;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        $"Expected '{metricName}' metric of type '{typeof(T)}' for column '{Column}' but none was found.");
                }
            }

            public IEnumerable<(string Name, T Metric)> TryGetMetrics<T>(string metricRoot)
            {
                foreach (var value in Metrics
                    .Where(metric => metric.Name.StartsWith(metricRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    if (value.Metric is T typedValue)
                    {
                        yield return (value.Name, typedValue);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Expected '{metricRoot}*' metrics of type '{typeof(T)}' for column '{Column}' but none were found.");
                    }
                }
            }
        }
    }
}
