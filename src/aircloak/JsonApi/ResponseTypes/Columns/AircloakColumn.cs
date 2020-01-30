namespace Aircloak.JsonApi.ResponseTypes
{
    public abstract class AircloakColumn<T>
    {
        public abstract bool IsSuppressed { get; }

        public abstract bool IsNull { get; }
    }
}