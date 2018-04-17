using System;
using System.Threading;

namespace Csi.Plugins.AzureDisk
{
    class Program
    {
        private const string envHost = "CSI_HOST";
        private const string envPort = "CSI_PORT";
        private const string defaultHost = "127.0.0.1";
        private const int defaultPort = 10000;
        static void Main(string[] args)
        {
            var host = Environment.GetEnvironmentVariable(envHost);
            if (string.IsNullOrEmpty(host)) host = defaultHost;

            int port = defaultPort;
            var portStr = Environment.GetEnvironmentVariable(envPort);
            if (!string.IsNullOrEmpty(portStr)) port = int.Parse(portStr);

            new AzureDiskCsiRpcServer().Start(host, port);
            Console.WriteLine("Server listening at {0}:{1}", host, port);

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
