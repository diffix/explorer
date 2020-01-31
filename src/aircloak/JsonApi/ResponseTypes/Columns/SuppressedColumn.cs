namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents a column value that has been suppressed by Diffix anonymization.
    /// </summary>
    /// <typeparam name="T">The expected type of the column value.</typeparam>
    public class SuppressedColumn<T> : AircloakColumn<T>
    {
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
    }
}
