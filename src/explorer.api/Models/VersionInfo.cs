namespace Explorer.Api
{
    internal sealed class VersionInfo
    {
        private const string ThisCommitRef = ThisAssembly.Git.Branch;

        private const string ThisCommitHash = ThisAssembly.Git.Sha;

        private VersionInfo(string commitHash, string commitRef)
        {
            CommitHash = commitHash;
            CommitRef = commitRef;
        }

        public string CommitRef { get; }

        public string CommitHash { get; }

        public static VersionInfo ForThisAssembly()
        {
            return new VersionInfo(ThisCommitHash, ThisCommitRef);
        }
    }
}