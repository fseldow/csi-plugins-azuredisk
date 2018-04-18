using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Csi.Plugins.AzureDisk
{
    sealed class AzureResourceInnerHelper
    {
        public static ResourceId CreateForDisk(string subscription, string resourceGroup, string resource)
            => ResourceId.FromString(
                ResourceUtils.ConstructResourceId(
                   subscription.ToLower(),
                   resourceGroup.ToLower(),
                   "Microsoft.Compute",
                   "disks",
                   resource.ToLower(),
                   null));
    }
}
