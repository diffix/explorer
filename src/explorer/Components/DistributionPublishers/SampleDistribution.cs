namespace Explorer.Components
{
    using System.Collections.Generic;

    public interface SampleDistribution<T>
    {
        public double Entropy { get; }

        public T Mean { get; }

        public T Mode { get; }

        public (T, T, T) Quartiles { get; }

        public double StandardDeviation { get; }

        public double Variance { get; }

        public IEnumerable<T> Generate(int numSamples);
    }
}