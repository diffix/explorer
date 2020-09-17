namespace Explorer.Common
{
    public interface CountableRow
    {
        long Count { get; }

        double? CountNoise { get; }

        bool IsNull { get; }

        bool IsSuppressed { get; }
    }
}