using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;

namespace Csi.Plugins.AzureDisk
{
    sealed class ManagedDiskSetupServiceVmss : IManagedDiskSetupService
    {
        private readonly IComputeManagementClient computeManagementClient;
        private readonly ILogger logger;
        private readonly IManagedDataDiskOperator managedDataDiskOperator = new ManagedDataDiskOperator();

        public ManagedDiskSetupServiceVmss(IComputeManagementClient computeManagementClient,
            ILogger<ManagedDiskSetupServiceVmss> logger)
        {
            this.computeManagementClient = computeManagementClient;
            this.logger = logger;
        }

        public async Task<ManagedDiskSetupInfo> AddAsync(ResourceId vmId, ResourceId managedDiskId)
        {
            int lun = -1;
            await updateVmssVm(vmId, vm =>
                lun = managedDataDiskOperator.AddDisk(vm.StorageProfile.DataDisks, managedDiskId));
            return new ManagedDiskSetupInfo
            {
                Lun = lun
            };
        }

        public Task RemoveAsync(ResourceId vmId, ResourceId managedDiskId)
        {
            return updateVmssVm(vmId, vm => managedDataDiskOperator.RemoveDisk(vm.StorageProfile.DataDisks, managedDiskId));
        }

        private async Task updateVmssVm(ResourceId vmId, Action<VirtualMachineScaleSetVM> action)
        {
            // TODO validate subscription
            var vm = await computeManagementClient.VirtualMachineScaleSetVMs.GetAsync(vmId.ResourceGroupName, vmId.Parent.Name, vmId.Name);
            action(vm);
            await computeManagementClient.VirtualMachineScaleSetVMs.UpdateAsync(vmId.ResourceGroupName, vmId.Parent.Name, vmId.Name, vm);
        }
    }
}
