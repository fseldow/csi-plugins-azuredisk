using System;
using System.Threading.Tasks;
using Csi.V0;
using Grpc.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    sealed class RpcControllerService : Controller.ControllerBase
    {
        private readonly IServiceClientCredentialsProvider provider;
        private readonly IManagedDiskConfigProvider contextConfig;
        private readonly IManagedDiskSetupServiceFactory setupServiceFactory;
        private readonly IManagedDiskProvisionServiceFactory provisionServiceFactory;
        private readonly ILogger logger;


        public RpcControllerService(
            IServiceClientCredentialsProvider provider,
            IManagedDiskConfigProvider contextConfig,
            IManagedDiskSetupServiceFactory setupServiceFactory,
            IManagedDiskProvisionServiceFactory provisionServiceFactory,
            ILogger<RpcControllerService> logger)
        {
            this.provider = provider;
            this.contextConfig = contextConfig;
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
                    var ctx = new Helpers.Azure.DataProviderContext<ManagedDiskConfig>();
                    await contextConfig.Provide(ctx);

                    var provisionService = provisionServiceFactory.Create(
                        provider.Provide(),
                        ctx.Result.SubscriptionId);
                    var md = await provisionService.CreateAsync(
                        ctx.Result.SubscriptionId,
                        ctx.Result.ResourceGroupName,
                        request.Name,
                        ctx.Result.Location,
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
                    var ctx = new Helpers.Azure.DataProviderContext<ManagedDiskConfig>();
                    await contextConfig.Provide(ctx);
                    var provisionService = provisionServiceFactory.Create(provider.Provide(), ctx.Result.SubscriptionId);
                    await provisionService.DeleteAsync(AzureResourceInnerHelper.CreateForDisk(
                        ctx.Result.SubscriptionId, ctx.Result.ResourceGroupName, id));
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
                    var ctx = new Helpers.Azure.DataProviderContext<ManagedDiskConfig>();
                    await contextConfig.Provide(ctx);
                    var setupService = setupServiceFactory.Create(provider.Provide(), ctx.Result.SubscriptionId);
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
                    var ctx = new Helpers.Azure.DataProviderContext<ManagedDiskConfig>();
                    await contextConfig.Provide(ctx);
                    var setupService = setupServiceFactory.Create(provider.Provide(), ctx.Result.SubscriptionId);
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
