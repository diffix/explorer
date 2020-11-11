namespace Explorer.Components.ResultTypes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json;
    using Explorer.Common;

    public class JointProbabilityMatrix
    {
        private const int LowCountBucketSize = 3;

        private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement;

        private static readonly NoisyCountComparer NoisyCountComparerInstance = new NoisyCountComparer();

        private readonly Random rng = new Random();

        private readonly Dictionary<Index, NoisyCount> counts = new Dictionary<Index, NoisyCount>();

        // A reverse lookup table mapping cumulative sample count to a combination of values.
        private ImmutableList<NoisyCount> cdf = ImmutableList<NoisyCount>.Empty;

        private ImmutableArray<Index> cdfReverseLookup = ImmutableArray<Index>.Empty;

        private bool cdfIsStale;

        private NoisyCount suppressedCount = NoisyCount.Zero;

        public JointProbabilityMatrix(IEnumerable<int> cardinalities)
        {
            Cardinalities = cardinalities.ToArray();
            Dimensions = Cardinalities.Count;
            TotalAvailableBuckets = Cardinalities.Aggregate(1, (a, b) => a * b);

            // The square root of the sum-of-squares of the cardinalities.
            // This approximates the number of buckets along the diagonal of the n-dimensional hypercube.
            DiagonalCount = Math.Sqrt(Cardinalities.Select(x => x * x).Sum());
        }

        public int NonZeroBucketCount { get => counts.Count; }

        public IList<int> Cardinalities { get; }

        public int Dimensions { get; }

        /// <summary>
        /// Gets a measure of how correlated the columns are based on their joint probabilities.
        /// </summary>
        public double CorrelationFactor
        {
            get
            {
                if (Dimensions == 1)
                {
                    return 0.0;
                }

                return Math.Pow(DiagonalCount / NonZeroBucketCountEstimate, 1.0 / Dimensions);
            }
        }

        public long NonZeroBucketCountEstimate => NonZeroBucketCount + (suppressedCount.Count / LowCountBucketSize);

        public long TotalAvailableBuckets { get; }

        public double NonZeroBucketRatio => NonZeroBucketCountEstimate / TotalAvailableBuckets;

        private NoisyCount TotalSampleCount { get; set; }

        private double DiagonalCount { get; }

        public void AddBucket<T>(IndexedGroupingSetsResultMulti<T> igsrm)
        {
            cdfIsStale = true;

            if (igsrm.IsSuppressed)
            {
                suppressedCount += NoisyCount.FromCountableRow(igsrm);
                return;
            }

            var key = new Index(igsrm.Values.Select(v => v.IsNull ? NullElement.Clone() : v.Value.Clone()));
            var value = NoisyCount.FromCountableRow(igsrm);

            counts[key] = value + counts.GetValueOrDefault(key, NoisyCount.Zero);
        }

        /// <summary>
        /// Generates a random sample of values based on the joint probabilities, *including* the possibility of a set
        /// of suppressed values.
        /// </summary>
        /// <returns>An IEnumerable of samples, or an empty IEnumerable if the values are suppressed. </returns>
        public IEnumerable<JsonElement> GetSample()
        {
            var index = RandomSampleIndex(suppressedCount);

            if (index >= cdfReverseLookup.Length)
            {
                return ImmutableArray<JsonElement>.Empty;
            }

            return cdfReverseLookup[index].Values;
        }

        /// <summary>
        /// Generates a random sample of values based on the joint probabilities, *excluding* the possibility of a set
        /// of suppressed values.
        /// </summary>
        /// <returns>An IEnumerable of samples. </returns>
        public IEnumerable<JsonElement> GetSampleUnsuppressed()
        {
            var index = RandomSampleIndex();

            if (index >= cdfReverseLookup.Length)
            {
                return cdfReverseLookup.Last().Values;
            }

            return cdfReverseLookup[index].Values;
        }

        private int RandomSampleIndex(NoisyCount? suppressedCount = null)
        {
            if (cdfIsStale)
            {
                RefreshCdfReverseLookup();
                TotalSampleCount = counts.Aggregate(NoisyCount.Zero, (sum, kv) => sum + kv.Value);

                cdfIsStale = false;
            }

            var range = TotalSampleCount + (suppressedCount ?? NoisyCount.Zero);
            var randomCount = NoisyCount.Noiseless(rng.NextLong(range.Count));

            var searchResult = cdf.BinarySearch(randomCount, NoisyCountComparerInstance);
            return searchResult < 0 ? ~searchResult : searchResult;
        }

        private void RefreshCdfReverseLookup()
        {
            // Note: The cdf currently ignores the probability of suppressed values.
            cdf = counts.Values.Aggregate(
                ImmutableList<NoisyCount>.Empty,
                (cdf, count) =>
                {
                    var newRunningTotal = cdf.LastOrDefault() + count;
                    return cdf.Add(newRunningTotal);
                });

            cdfReverseLookup = counts.Keys.ToImmutableArray();

            Debug.Assert(cdf.Count == cdfReverseLookup.Length, "cdf and reverseLookup are out of sync.");
        }

        private class NoisyCountComparer : Comparer<NoisyCount>
        {
            public override int Compare([AllowNull] NoisyCount left, [AllowNull] NoisyCount right)
                => left.Count.CompareTo(right.Count);
        }

        private class Index : IEquatable<Index>
        {
            public Index(IEnumerable<JsonElement> values)
            {
                Values = values.ToImmutableArray();
            }

            public ImmutableArray<JsonElement> Values { get; }

            public override int GetHashCode()
                => Values.Aggregate(0, (total, next) => HashCode.Combine(total, next));

            public bool Equals(Index? other) =>
                other is Index index &&
                Values.Length == index.Values.Length &&
                Values.Zip(index.Values).All(_ => _.First.Equals(_.Second));

            public override bool Equals(object? obj) => Equals(obj);
        }
    }
}
