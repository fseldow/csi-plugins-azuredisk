using System;
using System.Threading.Tasks;
using Csi.Helpers.Azure;
using Csi.V0;
using Csi.V0.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Csi.Plugins.AzureDisk
{
    sealed class AzureDiskCsiRpcServiceFactory : ICsiRpcServiceFactory
    {
        private readonly IServiceProvider serviceProvider;
        public AzureDiskCsiRpcServiceFactory()
        {
            serviceProvider = new ServiceCollection()
               .AddLogging(lb => lb.AddSerillogConsole())
               .AddSingleton<IManagedDiskProvisionServiceFactory, ManagedDiskProvisionServiceFactory>()
               .AddSingleton<IManagedDiskSetupServiceFactory, ManagedDiskSetupServiceFactoryStandalone>()
               .AddSingleton<IAzureDiskAttacher, AzureDiskAttacher>()
               .AddSingleton<IAzureDiskOperator, AzureDiskOperatorLinux>()
               .AddSingleton<IServiceClientCredentialsProvider, SpServiceClientCredentialsProvider>()
               .AddSingleton<IAzureSpProvider>(sp =>
               {
                   var env = ActivatorUtilities.CreateInstance<AzureSpProvider>(sp);
                   var sec = ActivatorUtilities.CreateInstance<AzureSpProviderFromSec>(sp);
                   sec.Inner = env;
                   return sec;
               })
               .AddSingleton<IManagedDiskConfigProvider>(sp =>
               {
                   var im = ActivatorUtilities.CreateInstance<ManageDiskConfigProviderInstanceMetadata>(sp);
                   var env = ActivatorUtilities.CreateInstance<ManagedDiskConfigProviderEnv>(sp);
                   env.Inner = im;
                   return env;
               })
               .AddExternalRunner()
               .AddInstanceMetadataService()
               .BuildServiceProvider();
        }

        public Identity.IdentityBase CreateIdentityRpcService() =>
            new IdentityRpcService(Constants.Name, Constants.Version);

        public Controller.ControllerBase CreateControllerRpcService() =>
            ActivatorUtilities.CreateInstance<RpcControllerService>(serviceProvider);

        public Node.NodeBase CreateNodeRpcService()
            => ActivatorUtilities.CreateInstance<RpcNodeService>(serviceProvider, getNodeIdFromEnv().Result);

        private async Task<string> getNodeIdFromEnv()
        {
            var env = Environment.GetEnvironmentVariable("NODE_ID");
            if (env != null) return env;
            var mes = await serviceProvider.GetRequiredService<IInstanceMetadataService>().GetInstanceMetadataAsync();
            if (mes != null) return mes.GetResourceId().Id;
            return Environment.MachineName;
        }
    }
}
