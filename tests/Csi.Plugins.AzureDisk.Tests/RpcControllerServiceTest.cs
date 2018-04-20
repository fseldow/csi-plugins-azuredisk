using System.Threading.Tasks;
using Csi.V0;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Csi.Plugins.AzureDisk.Tests
{
    public class RpcControllerServiceTest
    {
        [Fact]
        public async Task ControllerGetCapabilities()
        {
            var service = createService();

            var response = await service.ControllerGetCapabilities(new ControllerGetCapabilitiesRequest(), null);
            Assert.Equal(2, response.Capabilities.Count);
            Assert.Equal(ControllerServiceCapability.Types.RPC.Types.Type.CreateDeleteVolume,
                response.Capabilities[0].Rpc.Type);
            Assert.Equal(ControllerServiceCapability.Types.RPC.Types.Type.PublishUnpublishVolume,
                response.Capabilities[1].Rpc.Type);
        }

        [Fact]
        public async Task UnsupportedApiShouldThrowRpcUnimplementedException()
        {
            var service = createService();

            await AssertRpc.ThrowsRpcUnimplementedException(()
                => service.ValidateVolumeCapabilities(new ValidateVolumeCapabilitiesRequest(), null));

            await AssertRpc.ThrowsRpcUnimplementedException(()
                => service.ListVolumes(new ListVolumesRequest(), null));

            await AssertRpc.ThrowsRpcUnimplementedException(()
                => service.GetCapacity(new GetCapacityRequest(), null));

            await AssertRpc.ThrowsRpcUnimplementedException(()
                => service.GetCapacity(new GetCapacityRequest(), null));
        }

        private RpcControllerService createService()
        {
            var lf = new LoggerFactory();
            return new RpcControllerService(null, null, null, null, lf.CreateLogger<RpcControllerService>());
        }
    }
}
