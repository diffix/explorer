namespace Explorer.Common
{
    using System;

    internal struct NoisyCount : IEquatable<NoisyCount>
    {
        private NoisyCount(long count, double variance)
        {
            Count = count;
            Variance = variance;
        }

        public static NoisyCount Zero => default;

        public long Count { get; private set; }

        public double Variance { get; private set; }

        public double Noise => Math.Sqrt(Variance);

        public static bool operator ==(NoisyCount left, NoisyCount right) => left.Equals(right);

        public static bool operator !=(NoisyCount left, NoisyCount right) => !(left == right);

        public static NoisyCount operator +(NoisyCount left, NoisyCount right) => left.Add(right);

        public static NoisyCount Noiseless(long count) => new NoisyCount(count, 0);

        public static NoisyCount WithNoise(long count, double noise) => new NoisyCount(count, noise * noise);

        public static NoisyCount WithVariance(long count, double variance) => new NoisyCount(count, variance);

        public static NoisyCount FromCountableRow(CountableRow row) => WithNoise(row.Count, row.CountNoise ?? 0.0);

        public NoisyCount Add(NoisyCount other) => new NoisyCount(Count + other.Count, Variance + other.Variance);

        public void Add(long count, double noise)
        {
            Count += count;
            Variance += noise * noise;
        }

        public void Add(CountableRow row)
        {
            Add(row.Count, row.CountNoise ?? 0.0);
        }

        public override bool Equals(object? obj)
        {
            return obj is NoisyCount other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, Variance);
        }

        public bool Equals(NoisyCount other) =>
            Count == other.Count && Variance == other.Variance;
    }
}