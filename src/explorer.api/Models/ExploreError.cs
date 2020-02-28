namespace Explorer.Api.Models
{
    internal class ExploreError : ExploreResult
    {
        public ExploreError(System.Guid explorationId, string description)
            : base(explorationId, ExploreStatus.Error)
        {
            Description = description;
        }

        public string Description { get; }
    }
}