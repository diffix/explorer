namespace Diffix.Values
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents an unsuppressed NULL column value.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    internal sealed class NullValue<T> : DValue<T>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly DValue<T> Instance = new NullValue<T>();

        private static readonly string NullString = $"null [{typeof(T)}]";

        private NullValue()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// Always returns false because the column has not been suppressed by Diffix anonymization.
        /// </summary>
        public bool IsSuppressed => false;

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// Always returns true because the returned value is NULL.
        /// </summary>
        public bool IsNull => true;

        /// <summary>
        /// Gets a value indicating whether the column contained a valid value.
        /// Always returns false since the returned value is NULL.
        /// </summary>
        public bool HasValue => false;

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        /// <remarks>
        /// Throws an exception, since accessing the value is an invalid operation.
        /// </remarks>
        public T Value => throw new InvalidOperationException("Do not use NullValue.Value.");

        public override bool Equals(object? obj) => obj is NullValue<T>;

        public override int GetHashCode() => typeof(NullValue<T>).GetHashCode();

        public override string ToString() => NullString;

        private static string GetDebuggerDisplay() => NullString;
    }
}
