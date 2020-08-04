namespace Diffix.Values
{
    using Diffix;

    /// <summary>
    /// Represents a column value that has been suppressed by Diffix anonymization.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    internal sealed class SuppressedValue<T> : DValue<T>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly DValue<T> Instance = new SuppressedValue<T>();

        private SuppressedValue()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// Always returns true since this particular column has been suppressed.
        /// </summary>
        public bool IsSuppressed => true;

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// Always returns false since this particular column has been suppressed.
        /// </summary>
        public bool IsNull => false;

        /// <summary>
        /// Gets a value indicating whether the column contained a valid value.
        /// Always returns false since the column is suppressed.
        /// </summary>
        public bool HasValue => false;

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        /// <remarks>
        /// Throws an exception, since accessing the value is an invalid operation.
        /// </remarks>
        public T Value => throw new System.InvalidOperationException("Do not use SuppressedValue.Value.");
    }
}
