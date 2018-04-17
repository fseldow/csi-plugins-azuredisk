using System.Threading.Tasks;

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
        Task DeleteAsync(AzureResourceId managedDiskId);
    }

    sealed class ManagedDisk
    {
        public AzureResourceId Id { get; set; }
    }
}
