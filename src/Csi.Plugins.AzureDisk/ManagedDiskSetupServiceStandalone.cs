using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    sealed class ManagedDiskSetupServiceStandalone : IManagedDiskSetupService
    {
        private readonly IComputeManagementClient computeManagementClient;
        private readonly ILogger logger;
        private readonly IManagedDataDiskOperator managedDataDiskOperator = new ManagedDataDiskOperator();

        public ManagedDiskSetupServiceStandalone(
            IComputeManagementClient computeManagementClient,
            ILogger<ManagedDiskSetupServiceStandalone> logger)
        {
            this.computeManagementClient = computeManagementClient;
            this.logger = logger;
        }

        public async Task<ManagedDiskSetupInfo> AddAsync(ResourceId vmId, ResourceId managedDiskId)
        {
            int lun = -1;
            using (logger.BeginResourceIdScope("vm", vmId))
            using (logger.BeginResourceIdScope("manageddisk", managedDiskId))
            using (var s = logger.StepInformation("Add disk"))
            {
                await updateVm(vmId, 
                    vm => lun = managedDataDiskOperator.AddDisk(vm.StorageProfile.DataDisks, managedDiskId));

                logger.LogInformation("Setup at lun:{0}", lun);
                s.Commit();
            }

            return new ManagedDiskSetupInfo
            {
                Lun = lun,
            };
        }

        public async Task RemoveAsync(ResourceId vmId, ResourceId managedDiskId)
        {
            using (logger.BeginResourceIdScope("vm", vmId))
            using (logger.BeginResourceIdScope("manageddisk", managedDiskId))
            using (var s = logger.StepInformation("Remove disk"))
            {
                await updateVm(vmId, vm => managedDataDiskOperator.RemoveDisk(vm.StorageProfile.DataDisks, managedDiskId));
                s.Commit();
            }
        }

        private async Task updateVm(ResourceId vmId, Action<VirtualMachine> action)
        {
            // TODO validate subscription
            var vm = await computeManagementClient.VirtualMachines.GetAsync(vmId.ResourceGroupName, vmId.Name);
            action(vm);
            await computeManagementClient.VirtualMachines.CreateOrUpdateAsync(vmId.ResourceGroupName, vmId.Name, vm);
        }
    }


}
