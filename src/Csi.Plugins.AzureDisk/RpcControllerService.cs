using System;
using System.Threading.Tasks;
using Csi.V0;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    class ContextConfig
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }

        public static ContextConfig FromEnv()
        {
            return new ContextConfig
            {
                Subscription = Environment.GetEnvironmentVariable("DEFAULT_SUBSCRIPTION"),
                ResourceGroup = Environment.GetEnvironmentVariable("DEFAULT_RESOURCEGROUP"),
            };
        }
    }

    sealed class RpcControllerService : Controller.ControllerBase
    {
        private readonly IServiceClientCredentialsProvider provider = new ServiceClientCredentialsProvider();
        private readonly IManagedDiskProvisionServiceFactory factory;
        private readonly ILogger logger;
        private ContextConfig contextConfig = ContextConfig.FromEnv();


        public RpcControllerService(IManagedDiskProvisionServiceFactory factory, ILogger<RpcControllerService> logger)
        {
            this.factory = factory;
            this.logger = logger;
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
                    IManagedDiskProvisionService provisionService = factory.Create(provider.Provide(), contextConfig.Subscription);
                    await provisionService.CreateAsync(contextConfig.Subscription, contextConfig.ResourceGroup,
                        request.Name, "westus2", 3);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception in CreateAsync");
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
