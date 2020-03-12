namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents a column value that has been suppressed by Diffix anonymization.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    public sealed class SuppressedValue<T> : AircloakValue<T>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly AircloakValue<T> Instance = new SuppressedValue<T>();

        private SuppressedValue()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// Always returns true since this particular column has been suppressed.
        /// </summary>
        public override bool IsSuppressed => true;

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// Always returns false since this particular column has been suppressed.
        /// </summary>
        public override bool IsNull => false;

        /// <summary>
        /// Gets a value indicating whether the column contained a valid value.
        /// Always returns false since the column is suppressed.
        /// </summary>
        public override bool HasValue => false;

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        /// <remarks>
        /// Throws an exception, since accessing the value is an invalid operation.
        /// </remarks>
        public override T Value => throw new System.InvalidOperationException("Do not use NullValue.Value.");
    }
}
