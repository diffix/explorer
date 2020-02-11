namespace Aircloak.JsonApi
{
    using System.Text.Json;

    /// <summary>
    /// Factory interface for creating new <c>TRow</c> objects from Json arrays.
    /// </summary>
    /// <typeparam name="TRow">The target type that will be instantiated from the json array.</typeparam>
    public interface IRowReader<TRow>
    {
        /// <summary>
        /// Read the contents of a json array and return an instance of type <c>TRow</c>.
        /// </summary>
        /// <param name="reader">A ref to the <see cref="Utf8JsonReader"/> instance to read from.</param>
        /// <returns>An instance of <c>TRow</c>.</returns>
        public TRow FromJsonArray(ref Utf8JsonReader reader);
    }
}
