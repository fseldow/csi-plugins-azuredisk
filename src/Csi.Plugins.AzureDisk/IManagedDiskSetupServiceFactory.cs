using Microsoft.Azure.Management.Compute;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDiskSetupServiceFactory
    {
        IManagedDiskSetupService Create(
            ServiceClientCredentials serviceClientCredentials,
            string subscription);
    }

    class ManagedDiskSetupServiceFactoryStandalone : IManagedDiskSetupServiceFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public ManagedDiskSetupServiceFactoryStandalone(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public IManagedDiskSetupService Create(
            ServiceClientCredentials serviceClientCredentials,
            string subscription)
        {
            var cm = new ComputeManagementClient(serviceClientCredentials)
            {
                SubscriptionId = subscription,
            };

            return new ManagedDiskSetupServiceStandalone(
                cm,
                loggerFactory.CreateLogger<ManagedDiskSetupServiceStandalone>());
        }
    }
}
