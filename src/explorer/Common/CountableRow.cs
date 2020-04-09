namespace Explorer.Common
{
    internal interface CountableRow
    {
        long Count { get; }

        double? CountNoise { get; }

        bool IsNull { get; }

        bool IsSuppressed { get; }
    }
}