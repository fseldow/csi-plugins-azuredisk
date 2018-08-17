using System;
using Csi.Helpers.Azure;
using System.Collections.Generic;
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
        private readonly ILoggerFactory logger;
        private readonly ComputeManagementClient computeManagementClient;
   
        public TestAzureDisk()
        {
            //var TenantId = Environment.GetEnvironmentVariable("DEFAULT_TENANT_ID");
            //var ClientId = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_ID");
            //var ClientSecret = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_SECRET");

            var TenantId = Environment.GetEnvironmentVariable("K8S_AZURE_TENANTID");
            var ClientId = Environment.GetEnvironmentVariable("K8S_AZURE_SPID");
            var ClientSecret = Environment.GetEnvironmentVariable("K8S_AZURE_SPSEC");
            var subscription = "c4528d9e-c99a-48bb-b12d-fde2176a43b8";

            var ServiceClientCredentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                ClientId,
                ClientSecret,
                TenantId,
                AzureEnvironment.AzureGlobalCloud);
        
            this.computeManagementClient = new ComputeManagementClient(ServiceClientCredentials)
            {
                SubscriptionId = subscription,
            };
        }

        public async Task CreateDisk(string resourceGroup, string name){
            var disk = new Disk
                {
                    Location = "eastus",
                    DiskSizeGB = 2,
                    CreationData = new CreationData("Empty"),
                };
             await computeManagementClient.Disks.CreateOrUpdateAsync(resourceGroup, name, disk);
        }
        
    }
}