using System;
using Xunit;

namespace Csi.Plugins.AzureDisk.Tests
{
    public class AzureDiskCsiRpcServerTest
    {
        [Fact]
        public void TestGetService()
        {
            Environment.SetEnvironmentVariable("NODE_ID", "s01");
            var server = new AzureDiskCsiRpcServiceFactory();
            Assert.NotNull(server.CreateIdentityRpcService());
            Assert.NotNull(server.CreateControllerRpcService());
            Assert.NotNull(server.CreateNodeRpcService());
        }
    }
}
