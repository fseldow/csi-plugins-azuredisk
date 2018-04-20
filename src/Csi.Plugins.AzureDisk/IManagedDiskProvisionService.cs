using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDiskProvisionService
    {
        // size in GiB
        Task<ManagedDisk> CreateAsync(
            string subscription,
            string resourceGroup,
            string name,
            string location,
            int size);
        Task DeleteAsync(ResourceId managedDiskId);
    }

    sealed class ManagedDisk
    {
        public ResourceId Id { get; set; }
        public int Size { get; set; }
    }
}
