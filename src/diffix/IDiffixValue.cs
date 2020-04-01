namespace Diffix
{
    /// <summary>
    /// Interface for Diffix values which can be suppressed or NULL.
    /// </summary>
    public interface IDiffixValue
    {
        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// </summary>
        bool IsSuppressed { get; }

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// </summary>
        bool IsNull { get; }

        /// <summary>
        /// Gets a value indicating whether the column contained a valid value.
        /// </summary>
        bool HasValue => !(IsNull || IsSuppressed);
    }

    /// <summary>
    /// Interface for Diffix column values which can be suppressed, NULL or contain some data.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    public interface IDiffixValue<T> : IDiffixValue
    {
        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        T Value { get; }
    }
}