namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents an unsuppressed NULL column value.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    public class NullColumn<T> : AircloakColumn<T>
    {
        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// Always returns false because the column has not been suppressed by Diffix anonymization.
        /// </summary>
        public override bool IsSuppressed => false;

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// Always returns true because the returned value is NULL.
        /// </summary>
        public override bool IsNull => true;
    }
}
