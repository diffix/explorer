namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Abstract base class for Diffix column values which can be suppressed or NULL.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    public abstract class AircloakValue<T>
    {
        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// </summary>
        public abstract bool IsSuppressed { get; }

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// </summary>
        public abstract bool IsNull { get; }

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public abstract T Value { get; }
    }
}