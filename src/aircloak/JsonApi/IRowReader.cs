namespace Aircloak.JsonApi
{
    using System.Text.Json;

    public interface IRowReader<TRow>
    {
        public TRow FromJsonArray(ref Utf8JsonReader reader);
    }
}
