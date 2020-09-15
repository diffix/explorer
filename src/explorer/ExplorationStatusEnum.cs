namespace Explorer
{
    using System;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public static class ExplorationStatusEnum
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ExplorationStatus
        {
            /// <summary>
            /// Waiting to be run.
            /// </summary>
            New,

            /// <summary>
            /// Validating parameters.
            /// </summary>
            Validating,

            /// <summary>
            /// Running.
            /// </summary>
            Processing,

            /// <summary>
            /// Completed Successfully.
            /// </summary>
            Complete,

            /// <summary>
            /// Completed due to cancellation.
            /// </summary>
            Canceled,

            /// <summary>
            /// Completed with errors.
            /// </summary>
            Error,
        }

        public static ExplorationStatus ToExplorationStatus(this TaskStatus status) => status switch
        {
            TaskStatus.Canceled => ExplorationStatus.Canceled,
            TaskStatus.Created => ExplorationStatus.New,
            TaskStatus.Faulted => ExplorationStatus.Error,
            TaskStatus.RanToCompletion => ExplorationStatus.Complete,
            TaskStatus.Running => ExplorationStatus.Processing,
            TaskStatus.WaitingForActivation => ExplorationStatus.Processing,
            TaskStatus.WaitingToRun => ExplorationStatus.Processing,
            TaskStatus.WaitingForChildrenToComplete => ExplorationStatus.Processing,
            _ => throw new Exception("Unexpected TaskStatus: '{exploration.Status}'."),
        };

        public static bool IsComplete(this ExplorationStatus explorationStatus)
                => explorationStatus == ExplorationStatus.Complete ||
                    explorationStatus == ExplorationStatus.Error ||
                    explorationStatus == ExplorationStatus.Complete;
    }
}