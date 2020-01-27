namespace Aircloak.JsonApi
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// An interface for (de)serializing to and from an array of JSON values.
    /// </summary>
    /// <remarks>
    /// This interface is used by <see cref="JsonArrayConverter{T}"/> to derive the
    /// <see cref="JsonConverter"/> for the rows of query results returned from
    /// the Aircloak API.
    /// </remarks>
    public interface IJsonArrayConvertible
    {
        /// <summary>
        /// Reads values from a JSON array.
        /// </summary>
        /// <param name="reader">The reader of the JSON buffer.</param>
        /// <example>
        /// Read a double and a string into the MyDouble and MyString properties:
        /// <code>
        /// public void FromArrayValues(ref Utf8JsonReader reader) {
        ///     reader.Read();
        ///     MyDouble = reader.GetDouble();
        ///     reader.Read();
        ///     MyString = reader.GetString();
        /// }
        /// </code>
        /// </example>
        public void FromArrayValues(ref Utf8JsonReader reader);

        /// <summary>
        /// Writes values to a JSON array.
        /// </summary>
        /// <param name="writer">The writer to the JSON buffer.</param>
        /// <example>
        /// Write the properties MyDouble and MyString to the buffer:
        /// <code>
        /// public void FromArrayValues(ref Utf8JsonReader reader) {
        ///     writer.WriteNumberValue(MyDouble)
        ///     writer.WriteStringValue(MyString)
        /// }
        /// </code>
        /// </example>
        public void ToArrayValues(Utf8JsonWriter writer);
    }
}
