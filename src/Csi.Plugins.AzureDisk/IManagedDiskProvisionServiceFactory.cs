using System;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDiskProvisionServiceFactory
    {
        IManagedDiskProvisionService Create(ServiceClientCredentials serviceClientCredentials, string subscription);
    }


    class ManagedDiskProvisionServiceFactory : IManagedDiskProvisionServiceFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public ManagedDiskProvisionServiceFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public IManagedDiskProvisionService Create(
            ServiceClientCredentials serviceClientCredentials,
            string subscription)
        {
            var cm = new ComputeManagementClient(serviceClientCredentials)
            {
                SubscriptionId = subscription,
            };
            return new ManagedDiskProvisionService(cm, loggerFactory.CreateLogger<ManagedDiskProvisionService>());
        }
    }

    interface IServiceClientCredentialsProvider
    {
        ServiceClientCredentials Provide();
    }

    class SpServiceClientCredentialsProvider : IServiceClientCredentialsProvider
    {
        private readonly IAzureSpProvider azureSpProvider;

        public SpServiceClientCredentialsProvider(IAzureSpProvider azureSpProvider)
        {
            this.azureSpProvider = azureSpProvider;
        }

        public ServiceClientCredentials Provide()
        {
            var ctx = new Helpers.Azure.DataProviderContext<AzureAuthConfig> { };
            azureSpProvider.Provide(ctx);
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                ctx.Result.ClientId,
                ctx.Result.ClientSecret,
                ctx.Result.TenantId,
                AzureEnvironment.AzureGlobalCloud);
        }
    }
}
