using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Extensions.Logging;

namespace Csi.Plugins.AzureDisk
{
    sealed class ManagedDiskVmSetupServiceVmss : IManagedDiskVmSetupService
    {
        private readonly IComputeManagementClient computeManagementClient;
        private readonly ILogger logger;
        private readonly IManagedDataDiskOperator managedDataDiskOperator = new ManagedDataDiskOperator();

        public ManagedDiskVmSetupServiceVmss(IComputeManagementClient computeManagementClient,
            ILogger<ManagedDiskVmSetupServiceVmss> logger)
        {
            this.computeManagementClient = computeManagementClient;
            this.logger = logger;
        }

        public async Task AddAsync(AzureResourceId vmId, AzureResourceId managedDiskId)
        {
            await updateVmssVm(vmId, vm => managedDataDiskOperator.AddDisk(vm.StorageProfile.DataDisks, managedDiskId));
        }

        public Task RemoveAsync(AzureResourceId vmId, AzureResourceId managedDiskId)
        {
            return updateVmssVm(vmId, vm => managedDataDiskOperator.RemoveDisk(vm.StorageProfile.DataDisks, managedDiskId));
        }

        private async Task updateVmssVm(AzureResourceId vmId, Action<VirtualMachineScaleSetVM> action)
        {
            // TODO validate subscription
            var vm = await computeManagementClient.VirtualMachineScaleSetVMs.GetAsync(vmId.ResourceGroup, vmId.Resource, vmId.SubResource);
            action(vm);
            await computeManagementClient.VirtualMachineScaleSetVMs.UpdateAsync(vmId.ResourceGroup, vmId.Resource, vmId.SubResource, vm);
        }
    }
}
