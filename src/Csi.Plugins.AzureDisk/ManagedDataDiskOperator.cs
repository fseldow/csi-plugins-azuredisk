using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDataDiskOperator
    {
        int AddDisk(IList<DataDisk> disks, ResourceId managedDiskId);
        void RemoveDisk(IList<DataDisk> disks, ResourceId managedDiskId);
    }

    sealed class ManagedDataDiskOperator : IManagedDataDiskOperator
    {
        public int AddDisk(IList<DataDisk> disks, ResourceId managedDiskId)
        {
            var luns = disks.Select(d => d.Lun).ToArray();
            if (luns.Length >= 64) throw new Exception("No available lun found");
            int lun = 0;
            while (lun < 64 && luns.Contains(lun)) ++lun;

            DataDisk dataDisk = new DataDisk
            {
                Lun = lun,
                ManagedDisk = new ManagedDiskParameters
                {
                    Id = managedDiskId.Id,
                },
                CreateOption = "Attach",
            };
            disks.Add(dataDisk);

            return lun;
        }

        public void RemoveDisk(IList<DataDisk> disks, ResourceId managedDiskId)
        {
            var mid = managedDiskId.Id;
            var found = false;
            var i = 0;
            for (; i < disks.Count; i++)
            {
                if (disks[i].ManagedDisk.Id == mid) { found = true; break; }
            }
            if (found) disks.RemoveAt(i);
        }
    }
}
