namespace Explorer.Api
{
    internal sealed class VersionInfo
    {
        public const string ThisCommitRef = ThisAssembly.Git.Branch;

        private VersionInfo(string commitHash, string commitRef)
        {
            CommitHash = commitHash;
            CommitRef = commitRef;
        }

        public static string ThisCommitHash { get; } =
            ThisAssembly.Git.Sha + (ThisAssembly.Git.IsDirty ? "+" : string.Empty);

        public string CommitRef { get; }

        public string CommitHash { get; }

        public static VersionInfo ForThisAssembly()
        {
            return new VersionInfo(ThisCommitHash, ThisCommitRef);
        }
    }
}