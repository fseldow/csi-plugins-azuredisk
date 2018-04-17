using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Csi.V0;

namespace Csi.Plugins.AzureDisk.Tests
{
    public class AzureDiskNodeRpcServiceTest
    {
        [Fact]
        public async Task NodeGetId()
        {
            var service = createService();
            var response = await service.NodeGetId(new NodeGetIdRequest(), null);
            Assert.Equal("name", response.NodeId);
        }

        [Fact]
        public async Task NodeGetCapabilities()
        {
            var service = createService();
            var response = await service.NodeGetCapabilities(new NodeGetCapabilitiesRequest(), null);
            Assert.Empty(response.Capabilities);
        }

        [Fact]
        public async Task UnsupportedApiShouldThrowRpcUnimplementedException()
        {
            var service = createService();

            await AssertRpc.ThrowsRpcUnimplementedException(()
                => service.NodeStageVolume(new NodeStageVolumeRequest(), null));
            await AssertRpc.ThrowsRpcUnimplementedException(()
                => service.NodeUnstageVolume(new NodeUnstageVolumeRequest(), null));
        }

        private RpcNodeService createService()
        {
            var lf = new LoggerFactory();
            return new RpcNodeService(
                "name",
                lf.CreateLogger<RpcNodeService>());
        }
    }
}
