namespace Explorer
{
    using System;
    using System.Threading.Tasks;

    public static class ExplorationStatusConverter
    {
        public static ExplorationStatus FromTaskStatus(TaskStatus status) => status switch
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
    }
}
