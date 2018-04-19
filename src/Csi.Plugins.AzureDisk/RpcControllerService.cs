using System;
using System.Threading.Tasks;
using Csi.V0;
using Grpc.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    class ContextConfig
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string Location { get; set; }

        public static ContextConfig FromEnv()
        {
            return new ContextConfig
            {
                Subscription = Environment.GetEnvironmentVariable("DEFAULT_SUBSCRIPTION"),
                ResourceGroup = Environment.GetEnvironmentVariable("DEFAULT_RESOURCEGROUP"),
                Location = Environment.GetEnvironmentVariable("DEFAULT_LOCATION"),
            };
        }
    }

    sealed class RpcControllerService : Controller.ControllerBase
    {
        private readonly IServiceClientCredentialsProvider provider = new ServiceClientCredentialsProvider();
        private readonly IManagedDiskSetupServiceFactory setupServiceFactory;
        private readonly IManagedDiskProvisionServiceFactory provisionServiceFactory;
        private readonly ILogger logger;
        private ContextConfig contextConfig = ContextConfig.FromEnv();

        public RpcControllerService(
            IManagedDiskSetupServiceFactory setupServiceFactory,
            IManagedDiskProvisionServiceFactory provisionServiceFactory,
            ILogger<RpcControllerService> logger)
        {
            this.setupServiceFactory = setupServiceFactory;
            this.provisionServiceFactory = provisionServiceFactory;
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
                    IManagedDiskProvisionService provisionService = provisionServiceFactory.Create(provider.Provide(),
                        contextConfig.Subscription);
                    var md = await provisionService.CreateAsync(
                        contextConfig.Subscription,
                        contextConfig.ResourceGroup,
                        request.Name,
                        contextConfig.Location,
                        3);

                    response.Volume = new Volume
                    {
                        Id = md.Id.Id,
                        CapacityBytes = 3 << 30,
                    };
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
            using (logger.BeginKeyValueScope("volume_id", id))
            using (var _s = logger.StepInformation("{0}", nameof(DeleteVolume)))
            {
                try
                {
                    IManagedDiskProvisionService provisionService = provisionServiceFactory.Create(provider.Provide(), contextConfig.Subscription);
                    await provisionService.DeleteAsync(AzureResourceInnerHelper.CreateForDisk(contextConfig.Subscription, contextConfig.ResourceGroup, id));
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

        public override async Task<ControllerPublishVolumeResponse> ControllerPublishVolume(
            ControllerPublishVolumeRequest request,
            ServerCallContext context)
        {
            var response = new ControllerPublishVolumeResponse();

            var id = request.VolumeId;
            using (logger.BeginKeyValueScope("volume_id", id))
            using (var _s = logger.StepInformation("{0}", nameof(ControllerPublishVolume)))
            {
                try
                {
                    var setupService = setupServiceFactory.Create(provider.Provide(), contextConfig.Subscription);
                    var vmRid = ResourceId.FromString(request.NodeId);
                    var diskId = ResourceId.FromString(id);

                    var info = await setupService.AddAsync(vmRid, diskId);
                    response.PublishInfo.Add("lun", info.Lun.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception in ControllerPublishVolume");
                    throw new RpcException(new Status(StatusCode.Internal, ex.Message));
                }

                _s.Commit();
            }
            return response;
        }

        public override async Task<ControllerUnpublishVolumeResponse> ControllerUnpublishVolume(ControllerUnpublishVolumeRequest request, ServerCallContext context)
        {
            var response = new ControllerUnpublishVolumeResponse();

            var id = request.VolumeId;
            using (logger.BeginKeyValueScope("volume_id", id))
            using (var _s = logger.StepInformation("{0}", nameof(ControllerUnpublishVolume)))
            {
                try
                {
                    var setupService = setupServiceFactory.Create(provider.Provide(), contextConfig.Subscription);
                    var vmRid = ResourceId.FromString(request.NodeId);
                    var diskId = ResourceId.FromString(id);

                    await setupService.RemoveAsync(vmRid, diskId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception in ControllerUnpublishVolume");
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
                    },
                    new ControllerServiceCapability
                    {
                        Rpc = new ControllerServiceCapability.Types.RPC
                        {
                            Type = ControllerServiceCapability.Types.RPC.Types.Type.PublishUnpublishVolume
                        }
                    }
                }
            };

            return Task.FromResult(rp);
        }
    }
}
