namespace Explorer.Common.Utils
{
    public interface CountableRow
    {
        long Count { get; }

        double? CountNoise { get; }

        bool IsNull { get; }

        bool IsSuppressed { get; }
    }
}