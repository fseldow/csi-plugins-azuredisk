using Microsoft.Extensions.Logging;

namespace Csi.Plugins.AzureDisk
{
    sealed class AzureDiskServiceFactory : IAzureDiskServiceFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public AzureDiskServiceFactory(ILoggerFactory loggerFactory) => this.loggerFactory = loggerFactory;

        public IAzureDiskService Create()
        {
            return new AzureDiskService(loggerFactory.CreateLogger<AzureDiskService>());
        }
    }
}
