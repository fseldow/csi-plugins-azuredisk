using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Util.Extensions.Logging.Step;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Csi.Plugins.AzureDisk
{
    sealed class ManagedDiskProvisionService : IManagedDiskProvisionService
    {
        private readonly IComputeManagementClient computeManagementClient;
        private readonly ILogger logger;

        public ManagedDiskProvisionService(
            IComputeManagementClient computeManagementClient,
            ILogger<ManagedDiskProvisionService> logger)
        {
            this.computeManagementClient = computeManagementClient;
            this.logger = logger;
        }

        public async Task<ManagedDisk> CreateAsync(
            string subscription,
            string resourceGroup,
            string name,
            string location,
            int size)
        {
            var id = AzureResourceInnerHelper.CreateForDisk(subscription, resourceGroup, name);
            // TODO validate subscription
            using (logger.BeginResourceIdScope("manageddisk", id))
            using (var s = logger.StepInformation("Create managed disk: {0}", name))
            {
                var disk = new Disk
                {
                    Location = location,
                    DiskSizeGB = size,
                    CreationData = new CreationData("Empty"),
                };
                var dr = await computeManagementClient.Disks.CreateOrUpdateAsync(resourceGroup, name, disk);

                s.Commit();
            }

            return new ManagedDisk { Id = id };
        }

        public async Task DeleteAsync(ResourceId managedDiskId)
        {
            // TODO validate subscription
            using (logger.BeginResourceIdScope("manageddisk", managedDiskId))
            using (var s = logger.StepInformation("Delete managed disk"))
            {
                await computeManagementClient.Disks.DeleteAsync(managedDiskId.ResourceGroupName, managedDiskId.Name);
                s.Commit();
            }
        }
    }
}
