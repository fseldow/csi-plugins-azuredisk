using System.Threading.Tasks;

namespace Csi.Plugins.AzureDisk
{
    interface IAzureDiskAttacher
    {
        Task AttachAsync(string targetPath, int lun);
        Task DetachAsync(string targetPath);
    }
}
