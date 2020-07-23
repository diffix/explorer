namespace Explorer.Api
{
    using Explorer.Common;
    using Lamar;
    using Sentry;
    using Sentry.Extensibility;

    public class ExplorerEventProcessor : ISentryEventProcessor
    {
        private readonly IServiceContext serviceContext;

        public ExplorerEventProcessor(IServiceContext serviceContext)
        {
            this.serviceContext = serviceContext;
        }

        public SentryEvent Process(SentryEvent @event)
        {
            if (serviceContext.TryGetInstance<ExplorerContext>() is ExplorerContext ctx)
            {
                @event.SetTag("ColumnType", ctx.ColumnInfo.Type.ToString());
                @event.SetExtra("ExplorationContext", ctx);
            }

            @event.SetTag("GitSha", ThisAssembly.Git.Sha);
            @event.SetTag("GitBranch", ThisAssembly.Git.Branch);

            return @event;
        }
    }
}