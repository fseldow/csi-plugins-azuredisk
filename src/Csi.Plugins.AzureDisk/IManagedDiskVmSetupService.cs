using System.Threading.Tasks;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDiskVmSetupService
    {
        Task AddAsync(AzureResourceId vmId, AzureResourceId managedDiskId);
        Task RemoveAsync(AzureResourceId vmId, AzureResourceId managedDiskId);
    }
}
