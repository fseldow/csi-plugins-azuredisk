using System;
using System.Collections.Generic;
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


    class AzureAuthConfigProviderContext : DataProviderContext<AzureAuthConfig>
    {
        public IDictionary<string, string> Secrets { get; set; }
    }

    interface IAzureSpProvider : IDataProvider<AzureAuthConfig, AzureAuthConfigProviderContext> { }
    class AzureSpProvider : IAzureSpProvider
    {
        public Task Provide(AzureAuthConfigProviderContext context)
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

    class AzureSpProviderFromSec
        : ChainDataProvider<AzureAuthConfig, AzureAuthConfigProviderContext>,
        IAzureSpProvider
    {
        private const string nameTenantId = "tenantId";
        private const string nameClientId = "clientId";
        private const string nameClientSecret = "clientSecret";

        public override Task Provide(AzureAuthConfigProviderContext context)
        {
            if (context.Secrets.TryGetValue(nameTenantId, out var tenantId))
            {
                if (!context.Secrets.TryGetValue(nameClientId, out var clientId))
                    throw new Exception("No clientId provided");
                if (!context.Secrets.TryGetValue(nameClientSecret, out var clientSecret))
                    throw new Exception("No nameClientSecret provided");
                context.Result = new AzureAuthConfig
                {
                   TenantId = tenantId,
                   ClientId = clientId,
                   ClientSecret = clientSecret,
                };

                return Task.CompletedTask;
            }
            if (Inner != null) return Inner.Provide(context);
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

    class ManagedDiskConfigProviderEnv  
        : ChainDataProvider<ManagedDiskConfig, DataProviderContext<ManagedDiskConfig>>,
        IManagedDiskConfigProvider
    {
        public override Task Provide(DataProviderContext<ManagedDiskConfig> context)
        {
            var subsId = Environment.GetEnvironmentVariable("DEFAULT_SUBSCRIPTION");

            if (!string.IsNullOrEmpty(subsId)){
                context.Result = new ManagedDiskConfig
                {
                    SubscriptionId = subsId,
                    ResourceGroupName = Environment.GetEnvironmentVariable("DEFAULT_RESOURCEGROUP"),
                    Location = Environment.GetEnvironmentVariable("DEFAULT_LOCATION"),
                };
                return Task.CompletedTask;
            }
            if (Inner != null) return Inner.Provide(context);
            return Task.CompletedTask;
        }
    }
}
