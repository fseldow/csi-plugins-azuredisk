using System;
using System.Threading;
using Csi.V0.Server;

namespace Csi.Plugins.AzureDisk
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new CsiRpcServer(new AzureDiskCsiRpcServiceFactory());
            server.ConfigFromEnvironment();
            server.Start();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
