namespace Diffix
{
    /// <summary>
    /// The interface for Diffix column values which can be suppressed, NULL or contain some data.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    public interface DValue<T>
    {
        /// <summary>
        /// Singleton instance for null values.
        /// </summary>
        public static readonly DValue<T> Null = Values.NullValue<T>.Instance;

        /// <summary>
        /// Singleton instance for suppressed values.
        /// </summary>
        public static readonly DValue<T> Suppressed = Values.SuppressedValue<T>.Instance;

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// </summary>
        public bool IsSuppressed { get; }

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// </summary>
        public bool IsNull { get; }

        /// <summary>
        /// Gets a value indicating whether the column contained a valid value.
        /// </summary>
        public bool HasValue => !(IsNull || IsSuppressed);

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public T Value { get; }

#pragma warning disable CA1000 // do not declare static members on generic types
        /// <summary>
        /// Factory method that can be used to create objects that implement the <see cref="DValue{T}"/> interface.
        /// </summary>
        /// <param name="val">The contained value.</param>
        /// <returns>An object that impelemnts the <see cref="DValue{T}" /> interface and contains the given value.</returns>
        public static DValue<T> Create(T val) => new Values.DataValue<T>(val);
#pragma warning restore CA1000 // do not declare static members on generic types
    }
}