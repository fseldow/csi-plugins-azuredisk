using Microsoft.Extensions.Logging;

namespace Csi.Plugins.AzureDisk
{
    sealed class AzureDiskService : IAzureDiskService
    {
        private readonly ILogger logger;

        public AzureDiskService( ILogger<AzureDiskService> logger)
        {
            this.logger = logger;
        }
    }
}
