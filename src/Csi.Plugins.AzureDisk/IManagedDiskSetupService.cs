using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDiskSetupService
    {
        Task<ManagedDiskSetupInfo> AddAsync(ResourceId vmId, ResourceId managedDiskId);
        Task RemoveAsync(ResourceId vmId, ResourceId managedDiskId);
    }

    sealed class ManagedDiskSetupInfo
    {
        public int Lun { get; set; }
    }
}
