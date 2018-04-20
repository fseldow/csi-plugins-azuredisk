using System;
using System.Threading.Tasks;
using Csi.Helpers.Azure;

namespace Csi.Plugins.AzureDisk
{
    class AzureAuthConfig
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    class ManagedDiskConfig
    {
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Location { get; set; }
    }


    interface IAzureSpProvider : IDataProvider<AzureAuthConfig, DataProviderContext<AzureAuthConfig>> { }
    class AzureSpProvider : IAzureSpProvider
    {
        public Task Provide(DataProviderContext<AzureAuthConfig> context)
        {
            context.Result = new AzureAuthConfig
            {
                TenantId = Environment.GetEnvironmentVariable("DEFAULT_TENANT_ID"),
                ClientId = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_ID"),
                ClientSecret = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_SECRET"),
            };
            return Task.CompletedTask;
        }
    }

    interface IManagedDiskConfigProvider : IDataProvider<ManagedDiskConfig, DataProviderContext<ManagedDiskConfig>>
    {
    }

    class ManageDiskConfigProviderInstanceMetadata
        : ChainDataProvider<ManagedDiskConfig, DataProviderContext<ManagedDiskConfig>>,
        IManagedDiskConfigProvider
    {
        private readonly IInstanceMetadataService instanceMetadataService;

        public ManageDiskConfigProviderInstanceMetadata(IInstanceMetadataService instanceMetadataService)
        {
            this.instanceMetadataService = instanceMetadataService;
        }

        public override async Task Provide(DataProviderContext<ManagedDiskConfig> context)
        {
            var im = await instanceMetadataService.GetInstanceMetadataAsync();
            if (im != null)
            {
                context.Result = new ManagedDiskConfig
                {
                    SubscriptionId = im.Compute.SubscriptionId,
                    ResourceGroupName = im.Compute.ResourceGroupName,
                    Location = im.Compute.Location,
                };
                return;
            }

            if (Inner != null) await Inner.Provide(context);
        }
    }

    class ManagedDiskConfigProviderEnv : IManagedDiskConfigProvider
    {
        public Task Provide(DataProviderContext<ManagedDiskConfig> context)
        {
            context.Result = new ManagedDiskConfig
            {
                SubscriptionId = Environment.GetEnvironmentVariable("DEFAULT_SUBSCRIPTION"),
                ResourceGroupName = Environment.GetEnvironmentVariable("DEFAULT_RESOURCEGROUP"),
                Location = Environment.GetEnvironmentVariable("DEFAULT_LOCATION"),
            };
            return Task.CompletedTask;
        }
    }
}
