using System;
using System.Threading.Tasks;
using Csi.V0;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    sealed class RpcControllerService : Controller.ControllerBase
    {
        private readonly ILogger logger;

        public RpcControllerService(ILogger<RpcControllerService> logger)
        {
            this.logger = logger;

            logger.LogInformation("Rpc controller service loaded");
        }

        public override async Task<CreateVolumeResponse> CreateVolume(
            CreateVolumeRequest request,
            ServerCallContext context)
        {
            CreateVolumeResponse response = new CreateVolumeResponse();

            if (string.IsNullOrEmpty(request.Name))
            {
                logger.LogDebug("Validation fail");
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Name cannot be empty"));
            }

            using (var _s = logger.StepInformation("{0}, name: {1}", nameof(CreateVolume), request.Name))
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception in CreateVolume");
                    throw new RpcException(new Status(StatusCode.Internal, ex.Message));
                }

                _s.Commit();
            }

            return response;
        }

        public override async Task<DeleteVolumeResponse> DeleteVolume(
            DeleteVolumeRequest request,
            ServerCallContext context)
        {
            DeleteVolumeResponse response = new DeleteVolumeResponse();
            var id = request.VolumeId;
            using (var _s = logger.StepInformation("{0}, id: {1}", nameof(DeleteVolume), id))
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception in DeleteVolume");
                    throw new RpcException(new Status(StatusCode.Internal, ex.Message));
                }

                _s.Commit();
            }

            return response;
        }

        public override Task<ControllerGetCapabilitiesResponse> ControllerGetCapabilities(
            ControllerGetCapabilitiesRequest request, ServerCallContext context)
        {
            logger.LogInformation(nameof(ControllerGetCapabilities));

            var rp = new ControllerGetCapabilitiesResponse
            {
                Capabilities =
                {
                    new ControllerServiceCapability
                    {
                        Rpc = new ControllerServiceCapability.Types.RPC
                        {
                            Type = ControllerServiceCapability.Types.RPC.Types.Type.CreateDeleteVolume
                        }
                    }
                }
            };

            return Task.FromResult(rp);
        }
    }
}
