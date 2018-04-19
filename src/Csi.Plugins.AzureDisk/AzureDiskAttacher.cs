using System.Threading.Tasks;
using Csi.Helpers.Azure;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    class AzureDiskAttacher : IAzureDiskAttacher
    {
        private readonly IAzureDiskOperator azureDiskOperator;
        private readonly ILogger logger;

        public AzureDiskAttacher(IAzureDiskOperator azureDiskOperator, ILogger<AzureDiskAttacher> logger)
        {
            this.azureDiskOperator = azureDiskOperator;
            this.logger = logger;
        }

        public async Task AttachAsync(string targetPath, int lun)
        {
            using (var _s = logger.StepDebug(nameof(AttachAsync)))
            {
                var deviceId = await azureDiskOperator.ProbeDevice(lun);
                await azureDiskOperator.EnsureFormat(deviceId);
                await azureDiskOperator.Attach(targetPath, deviceId);

                _s.Commit();
            }
        }

        public async Task DetachAsync(string targetPath)
        {
            using (var _s = logger.StepDebug(nameof(DetachAsync)))
            {
                await azureDiskOperator.Detach(targetPath);
                _s.Commit();
            }
        }
    }
}
