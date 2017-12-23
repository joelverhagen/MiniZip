using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyVersion(
    ThisAssembly.Git.BaseVersion.Major + "." +
    ThisAssembly.Git.BaseVersion.Minor + "." +
    ThisAssembly.Git.BaseVersion.Patch)]

[assembly: AssemblyFileVersion(
    ThisAssembly.Git.BaseVersion.Major + "." +
    ThisAssembly.Git.BaseVersion.Minor + "." +
    ThisAssembly.Git.BaseVersion.Patch + ".0")]

[assembly: AssemblyInformationalVersion(
  ThisAssembly.Git.BaseVersion.Major + "." +
  ThisAssembly.Git.BaseVersion.Minor + "." +
  ThisAssembly.Git.BaseVersion.Patch +
  ThisAssembly.Git.SemVer.DashLabel)]

[assembly: AssemblyMetadata("CommitHash", ThisAssembly.Git.Sha)]
[assembly: AssemblyMetadata("Branch", ThisAssembly.Git.Branch)]
[assembly: AssemblyMetadata("IsDirty", ThisAssembly.Git.IsDirtyString)]
[assembly: AssemblyMetadata("Tag", ThisAssembly.Git.Tag)]

[assembly:InternalsVisibleTo("Knapcode.MiniZip.Test")]
