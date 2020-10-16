namespace Diffix.Values
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Represents an unsuppressed column value.
    /// </summary>
    /// <typeparam name="T">The type of the column value.</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    internal class DataValue<T> : DValue<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataValue{T}"/> class.
        /// </summary>
        /// <param name="columnValue">The column's value.</param>
        public DataValue(T columnValue)
        {
            Value = columnValue;
        }

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// Always returns false since the column is not suppressed.
        /// </summary>
        public bool IsSuppressed => false;

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// Always returns false since the column has a value.
        /// </summary>
        public bool IsNull => false;

        /// <summary>
        /// Gets a value indicating whether the column contained a valid value.
        /// Always returns true since the column has a value.
        /// </summary>
        public bool HasValue => true;

        /// <summary>
        /// Gets the column's value.
        /// </summary>
        public T Value { get; }

        public override bool Equals(object? obj) => obj is DataValue<T> value &&
                   EqualityComparer<T>.Default.Equals(Value, value.Value);

        public override int GetHashCode() => HashCode.Combine(typeof(DataValue<T>), Value);

        public override string ToString() => $"{Value} [{typeof(T)}]";

        private string GetDebuggerDisplay() => ToString();
    }
}
