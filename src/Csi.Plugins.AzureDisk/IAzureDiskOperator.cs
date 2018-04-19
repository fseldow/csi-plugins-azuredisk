using System;
using System.IO;
using System.Threading.Tasks;
using Csi.Helpers.Azure;
using Microsoft.Extensions.Logging;

namespace Csi.Plugins.AzureDisk
{
    interface IAzureDiskOperator
    {
        Task<string> ProbeDevice(int lun);
        Task EnsureFormat(string deviceId);
        Task Attach(string targetPath, string deviceId);
        Task Detach(string targetPath);
    }

    class AzureDiskOperatorLinux : IAzureDiskOperator
    {
        private const string pathBase = "/dev/disk/azure/scsi1/lun";
        private readonly IExternalRunner runner;
        private readonly ILogger logger;

        public AzureDiskOperatorLinux(
            IExternalRunnerFactory runnerFactory,
            ILogger<AzureDiskOperatorLinux> logger)
        {
            this.runner = runnerFactory.Create(true, true);
            this.logger = logger;
        }

        public Task<string> ProbeDevice(int lun)
        {
            var path = getPathByLun(lun);
            logger.LogDebug("Using path {0}", path);
            if (!File.Exists(path)) throw new Exception("Probe failed for lun: " + lun);

            return Task.FromResult(path);
        }

        public Task Attach(string targetPath, string deviceId)
        {
            return runner.RunExecutable("mount", deviceId, targetPath);
        }

        public Task Detach(string targetPath)
        {
            return runner.RunExecutable("umount", targetPath);
        }

        public async Task EnsureFormat(string deviceId)
        {
            var cmd = $"[ ! -z $(lsblk -ndo FSTYPE {deviceId}) ]";
            var exitCode = await runner.RunBash(cmd);
            if (exitCode != 0)
            {
                logger.LogInformation("Format disk");
                await runner.RunExecutable("mkfs.ext4", deviceId);
            }
        }

        private string getPathByLun(int lun)
            => pathBase + lun;
    }
}
