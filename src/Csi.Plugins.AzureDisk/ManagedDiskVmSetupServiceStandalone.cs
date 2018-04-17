using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    sealed class ManagedDiskVmSetupServiceStandalone : IManagedDiskVmSetupService
    {
        private readonly IComputeManagementClient computeManagementClient;
        private readonly ILogger logger;
        private readonly IManagedDataDiskOperator managedDataDiskOperator = new ManagedDataDiskOperator();

        public ManagedDiskVmSetupServiceStandalone(
            IComputeManagementClient computeManagementClient,
            ILogger<ManagedDiskVmSetupServiceStandalone> logger)
        {
            this.computeManagementClient = computeManagementClient;
            this.logger = logger;
        }

        public async Task AddAsync(AzureResourceId vmId, AzureResourceId managedDiskId)
        {
            using(logger.BeginScope("vmId", vmId.ToString()))
            using (var s = logger.StepInformation("Add disk"))
            {
                await updateVm(vmId, vm => managedDataDiskOperator.AddDisk(vm.StorageProfile.DataDisks, managedDiskId));
                s.Commit();
            }
        }

        public async Task RemoveAsync(AzureResourceId vmId, AzureResourceId managedDiskId)
        {
            await updateVm(vmId, vm => managedDataDiskOperator.RemoveDisk(vm.StorageProfile.DataDisks, managedDiskId));
        }

        private async Task updateVm(AzureResourceId vmId, Action<VirtualMachine> action)
        {
            // TODO validate subscription
            var vm = await computeManagementClient.VirtualMachines.GetAsync(vmId.ResourceGroup, vmId.Resource);
            action(vm);
            await computeManagementClient.VirtualMachines.CreateOrUpdateAsync(vmId.ResourceGroup, vmId.Resource, vm);
        }
    }


}
