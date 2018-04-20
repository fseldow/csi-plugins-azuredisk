using System;
using System.Threading.Tasks;
using Csi.Helpers.Azure;
using Csi.V0;
using Csi.V0.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Csi.Plugins.AzureDisk
{
    sealed class AzureDiskCsiRpcServer : CsiRpcServer
    {
        private readonly IServiceProvider serviceProvider;
        public AzureDiskCsiRpcServer()
        {
            serviceProvider = new ServiceCollection()
               .AddLogging(lb => lb.AddSerillogConsole())
               .AddSingleton<IManagedDiskProvisionServiceFactory, ManagedDiskProvisionServiceFactory>()
               .AddSingleton<IManagedDiskSetupServiceFactory, ManagedDiskSetupServiceFactoryStandalone>()
               .AddSingleton<IAzureDiskAttacher, AzureDiskAttacher>()
               .AddSingleton<IAzureDiskOperator, AzureDiskOperatorLinux>()
               .AddSingleton<IServiceClientCredentialsProvider, SpServiceClientCredentialsProvider>()
               .AddSingleton<IAzureSpProvider, AzureSpProvider>()
               .AddSingleton<IManagedDiskConfigProvider, ManagedDiskConfigProviderEnv>()
               .AddExternalRunner()
               .AddInstanceMetadataService()
               .BuildServiceProvider();
        }

        public override Identity.IdentityBase CreateIdentityRpcService() =>
            new IdentityRpcService(Constants.Name, Constants.Version);

        public override Controller.ControllerBase CreateControllerRpcService() =>
            ActivatorUtilities.CreateInstance<RpcControllerService>(serviceProvider);

        public override Node.NodeBase CreateNodeRpcService()
            => ActivatorUtilities.CreateInstance<RpcNodeService>(serviceProvider, getNodeIdFromEnv().Result);

        private async Task<string> getNodeIdFromEnv()
        {
            var env = Environment.GetEnvironmentVariable("NODE_ID");
            if (env != null) return env;
            var mes= await serviceProvider.GetRequiredService<IInstanceMetadataService>().GetInstanceMetadataAsync();
            if (mes != null) return mes.GetResourceId().Id;
            return Environment.MachineName;
        }
    }
}
