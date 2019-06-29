using System;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Knapcode.MiniZip
{
    public static class NuGetUtility
    {
        public static async Task<Uri> GetNupkgUrlAsync(string id, string version)
        {
            var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            var packageBaseAddress = serviceIndex.GetServiceEntryUri(ServiceTypes.PackageBaseAddress);

            var lowerId = id.ToLowerInvariant();
            var lowerVersion = NuGetVersion.Parse(version).ToNormalizedString().ToLowerInvariant();
            var packageUri = new Uri(packageBaseAddress, $"{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg");

            return packageUri;
        }
    }
}
