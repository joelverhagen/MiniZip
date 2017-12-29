using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyVersion(
    ThisAssembly.Git.SemVer.Major + "." +
    ThisAssembly.Git.SemVer.Minor + "." +
    ThisAssembly.Git.SemVer.Patch)]

[assembly: AssemblyFileVersion(
    ThisAssembly.Git.SemVer.Major + "." +
    ThisAssembly.Git.SemVer.Minor + "." +
    ThisAssembly.Git.SemVer.Patch + ".0")]

[assembly: AssemblyInformationalVersion(
  ThisAssembly.Git.SemVer.Major + "." +
  ThisAssembly.Git.SemVer.Minor + "." +
  ThisAssembly.Git.SemVer.Patch +
  ThisAssembly.Git.SemVer.DashLabel)]

[assembly: AssemblyMetadata("CommitHash", ThisAssembly.Git.Sha)]
[assembly: AssemblyMetadata("Branch", ThisAssembly.Git.Branch)]
[assembly: AssemblyMetadata("IsDirty", ThisAssembly.Git.IsDirtyString)]

[assembly:InternalsVisibleTo("Knapcode.MiniZip.Test")]
