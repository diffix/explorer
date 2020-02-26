namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents an unsuppressed column value.
    /// </summary>
    /// <typeparam name="T">The type of the column value.</typeparam>
    public class DataValue<T> : AircloakValue<T>
    {
        private T value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataValue{T}"/> class.
        /// </summary>
        /// <param name="columnValue">The column's value.</param>
        public DataValue(T columnValue)
        {
            value = columnValue;
        }

        /// <summary>
        /// Gets a value indicating whether the column value was suppressed.
        /// Always returns false since the column is not suppressed.
        /// </summary>
        public override bool IsSuppressed => false;

        /// <summary>
        /// Gets a value indicating whether the column value was NULL.
        /// Always returns false since the column has a value.
        /// </summary>
        public override bool IsNull => false;

        /// <summary>
        /// Gets the column's value.
        /// </summary>
        public override T Value => value;
    }
}
