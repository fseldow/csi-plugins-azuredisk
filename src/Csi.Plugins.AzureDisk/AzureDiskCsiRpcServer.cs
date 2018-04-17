using System;
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
               .AddSingleton<IAzureDiskServiceFactory, AzureDiskServiceFactory>()
               .BuildServiceProvider();
        }

        public override Identity.IdentityBase CreateIdentityRpcService() =>
            new IdentityRpcService(Constants.Name, Constants.Version);

        public override Controller.ControllerBase CreateControllerRpcService() =>
            ActivatorUtilities.CreateInstance<RpcControllerService>(serviceProvider);

        public override Node.NodeBase CreateNodeRpcService()
            => ActivatorUtilities.CreateInstance<RpcNodeService>(serviceProvider, getNodeIdFromEnv());

        private static string getNodeIdFromEnv() => Environment.GetEnvironmentVariable("NODE_ID") 
            ?? Environment.MachineName;
    }
}
