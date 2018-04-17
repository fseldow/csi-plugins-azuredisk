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

    class ServiceClientCredentialsProvider : IServiceClientCredentialsProvider
    {
        public ServiceClientCredentials Provide()
        {
            var clientId = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("DEFAULT_TENANT_ID");
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                clientId,
                clientSecret,
                tenantId,
                AzureEnvironment.AzureGlobalCloud);
        }
    }
}
