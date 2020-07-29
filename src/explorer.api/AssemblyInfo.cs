using System.Reflection;

// Set version number for the assembly.
[assembly: AssemblyVersion(
    ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + ".0")]
[assembly: AssemblyInformationalVersion(ThisAssembly.Git.Tag)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("explorer.api.tests")]