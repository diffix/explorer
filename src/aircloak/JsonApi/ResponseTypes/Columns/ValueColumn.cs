namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Text.Json;

    public class ValueColumn<T> : AircloakColumn<T>
    {
        public ValueColumn(T columnValue)
        {
            ColumnValue = columnValue;
        }

        public override bool IsSuppressed => false;

        public override bool IsNull => false;

        public T ColumnValue { get; private set; }
    }
}
