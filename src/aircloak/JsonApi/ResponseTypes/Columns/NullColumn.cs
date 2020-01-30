namespace Aircloak.JsonApi.ResponseTypes
{
    public class NullColumn<T> : AircloakColumn<T>
    {
        public override bool IsSuppressed => false;

        public override bool IsNull => true;
    }
}
