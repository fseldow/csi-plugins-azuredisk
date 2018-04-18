using System.Threading.Tasks;
using Csi.Helpers.Azure;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    class AzureDiskAttacherLinux : IAzureDiskAttacher
    {
        private readonly IExternalRunner cmdRunner;
        private readonly ILogger logger;

        public AzureDiskAttacherLinux(IExternalRunner cmdRunner, ILogger<AzureDiskAttacherLinux> logger)
        {
            this.cmdRunner = cmdRunner;
            this.logger = logger;
        }

        public async Task AttachAsync(string targetPath, int lun)
        {
            using (var _s = logger.StepDebug(nameof(AttachAsync)))
            {
                var lunLink = "/dev/disk/azure/scsi1/lun" + lun;
                await cmdRunner.RunExecutable("readlink", targetPath);
                _s.Commit();
            }
        }

        public async Task DetachAsync(string targetPath)
        {
            using (var _s = logger.StepDebug(nameof(DetachAsync)))
            {
                await cmdRunner.RunExecutable("umount", targetPath);
                _s.Commit();
            }
        }
    }
}
