namespace Explorer.Components.ResultTypes
{
    public class NumericColumnBounds
    {
        internal NumericColumnBounds(decimal min, decimal max)
        {
            Max = max;
            Min = min;
        }

        public decimal Min { get; }

        public decimal Max { get; }

        public bool IsZero => Min == 0 && Max == 0;
    }
}