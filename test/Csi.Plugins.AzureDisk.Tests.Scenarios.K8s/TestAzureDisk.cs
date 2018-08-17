using System;
using System.Net.Http;
using Csi.Helpers.Azure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;

namespace Csi.Plugins.AzureDisk.Tests.Scenarios.K8s
{
    public class TestAzureDisk
    {
        private readonly ComputeManagementClient computeManagementClient;
        private readonly string resourceGroup;
        private readonly ILogger logger;
   
        public TestAzureDisk(ILogger<TestAzureDisk> logger)
        {
            var TenantId = Environment.GetEnvironmentVariable("DEFAULT_TENANT_ID");
            var ClientId = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_ID");
            var ClientSecret = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_SECRET");
            var subsId = Environment.GetEnvironmentVariable("DEFAULT_SUBSCRIPTION");
            this.resourceGroup = Environment.GetEnvironmentVariable("DEFAULT_RESOURCEGROUP");

            this.logger = logger;

            var ServiceClientCredentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                ClientId,
                ClientSecret,
                TenantId,
                AzureEnvironment.AzureGlobalCloud);
        
            this.computeManagementClient = new ComputeManagementClient(ServiceClientCredentials, new Handler(logger))
            {
                SubscriptionId = subsId,
            };
        }

        public async Task CreateDisk(string name){
            var disk = new Disk
                {
                    Location = "eastus",
                    DiskSizeGB = 2,
                    CreationData = new CreationData("Empty"),
                };
             await computeManagementClient.Disks.CreateOrUpdateAsync(resourceGroup, name, disk);
        }

        public async Task<string> GetDiskResourceID(string name){
            var disk = await computeManagementClient.Disks.GetAsync(resourceGroup, name);
            return disk.Id;
        }

        public async Task<List<String>> GetDiskNameList(){
            var diskList = await computeManagementClient.Disks.ListByResourceGroupAsync(resourceGroup);
            var nameList = new List<string>();
            foreach(var disk in diskList){
                nameList.Add(disk.Name);
            }
            return nameList;
        }

        public async Task<bool> ValidateDiskDeleted(string diskName){
            bool isDeleted = false;
            for (int i =0; i<= 10 *60; i+=5){
                Thread.Sleep(5*1000);
                var diskNameList = await GetDiskNameList();
                if(diskNameList.Contains(diskName)){
                    continue;
                }
                isDeleted = true;
                break;
            }
            return isDeleted;
        }
    }
}