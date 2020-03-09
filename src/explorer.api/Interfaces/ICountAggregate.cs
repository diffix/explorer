namespace Explorer.Diffix.Interfaces
{
    internal interface ICountAggregate
    {
        public long Count { get; }

        public double? CountNoise { get; }
    }
}