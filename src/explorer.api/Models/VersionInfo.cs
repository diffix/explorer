namespace Explorer.Api
{
    public class VersionInfo
    {
        public VersionInfo(ExplorerConfig config)
        {
            CommitHash = config.CommitHash;
            CommitRef = config.CommitRef;
        }

        public string CommitHash { get; }

        public string CommitRef { get; }
    }
}