using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.Compute.Models;

namespace Csi.Plugins.AzureDisk
{
    interface IManagedDataDiskOperator
    {
        void AddDisk(IList<DataDisk> disks, AzureResourceId managedDiskId);
        void RemoveDisk(IList<DataDisk> disks, AzureResourceId managedDiskId);
    }

    sealed class ManagedDataDiskOperator : IManagedDataDiskOperator
    {
        public void AddDisk(IList<DataDisk> disks, AzureResourceId managedDiskId)
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
                    Id = managedDiskId.ToString(),
                },
                CreateOption = "Attach",
            };
            disks.Add(dataDisk);
        }

        public void RemoveDisk(IList<DataDisk> disks, AzureResourceId managedDiskId)
        {
            var mid = managedDiskId.ToString();
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
