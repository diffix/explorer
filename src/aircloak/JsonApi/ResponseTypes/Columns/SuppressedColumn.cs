namespace Aircloak.JsonApi.ResponseTypes
{
    public class SuppressedColumn<T> : AircloakColumn<T>
    {
        public override bool IsSuppressed => true;

        public override bool IsNull => false;
    }
}
