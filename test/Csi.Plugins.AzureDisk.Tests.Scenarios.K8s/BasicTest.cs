using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading;
using System.Diagnostics;


namespace Csi.Plugins.AzureDisk.Tests.Scenarios.K8s
{
    public class BasicTest: IDisposable
    {
        private static readonly ILoggerFactory loggerFactory = TestHelper.CreateLoggerFactory();
        private readonly ILogger logger;
        private TestKubernetesClient tkc;
        private TestAzureDisk azureDiskClient;

        public BasicTest(){
            this.logger = loggerFactory.CreateLogger<BasicTest>();
            var testNamespace = "e2e-tests-csi-disk" + Guid.NewGuid().ToString();
            this.tkc = new TestKubernetesClient(testNamespace, loggerFactory.CreateLogger<TestKubernetesClient>());
            this.azureDiskClient = new TestAzureDisk(loggerFactory.CreateLogger<TestAzureDisk>());
        }

        [Fact]
        public async Task CsiAzureDiskTest()
        {
            STEP("Init Environment and attach disk via csi plugins");
            var pvcName = "pvc1";
            var pvc = new V1PersistentVolumeClaim
            {
                Metadata = new V1ObjectMeta
                {
                    Name = pvcName,
                },
                Spec = new V1PersistentVolumeClaimSpec
                {
                    AccessModes = new List<string> { "ReadWriteOnce" },
                    StorageClassName = "azuredisk-csi",
                    Resources = new V1ResourceRequirements
                    {
                        Requests = new Dictionary<string, ResourceQuantity>
                        {
                            ["storage"] = new ResourceQuantity("2Gi"),
                        },
                    },
                },
            };
            var pod = new V1Pod
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "claim",
                },
                Spec = new V1PodSpec
                {
                    RestartPolicy = "Never",
                    Containers = new List<V1Container>
                    {
                        new V1Container
                        {
                            Image = "busybox",
                            Args = new List<string>{"touch", "/af-vol/temp"},
                            Name ="test",
                            VolumeMounts = new List<V1VolumeMount>
                            {
                                new V1VolumeMount
                                {
                                    MountPath ="/af-vol",
                                    Name="af-pvc",
                                }
                            }
                        }
                    },
                    Volumes = new List<V1Volume>
                    {
                        new V1Volume
                        {
                            Name = "af-pvc",
                            PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                            {
                                ClaimName = pvcName
                            },
                        }
                    }
                }
            };
            var diskListOld = await azureDiskClient.GetDiskNameList();
            await tkc.EnsureNamespace();
            await tkc.CreatePvc(pvc);
            await tkc.CreatePod(pod);
            Assert.True(await tkc.WaitPodCompleted(pod.Metadata.Name), "Pod construction failed");

            STEP("Geting the name of the new disk");
            var diskListNew = await azureDiskClient.GetDiskNameList();
            Assert.Equal(diskListOld.Count + 1, diskListNew.Count);
            string diskNewName = "";
            foreach(var diskNew in diskListNew){
                if(!diskListOld.Contains(diskNew)){
                    diskNewName = diskNew;
                    break;
                }
            }
            // Assume disk name is same as new pv
            var pvNewName = diskNewName;
            var newDiskID = await azureDiskClient.GetDiskResourceID(diskNewName);

            STEP("Unattach the disk");
            await tkc.ReplacePvReclaimPolicy(pvNewName, "Retain");
            await tkc.DeletePod(pod.Metadata.Name);
            await tkc.DeletePvc(pvcName);

            STEP("Building and attach the disk to a new pod");
            var pod2 = new V1Pod
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "claim2",
                },
                Spec = new V1PodSpec
                {
                    RestartPolicy = "Never",
                    Containers = new List<V1Container>
                    {
                        new V1Container
                        {
                            Image = "busybox",
                            Args = new List<string>{"ls", "./af-vol"},
                            Name ="test",
                            VolumeMounts = new List<V1VolumeMount>
                            {
                                new V1VolumeMount
                                {
                                    MountPath ="/af-vol",
                                    Name="af-pvc",
                                }
                            }
                        }
                    },
                    Volumes = new List<V1Volume>
                    {
                        new V1Volume
                        {
                            Name = "af-pvc",
                            AzureDisk = new V1AzureDiskVolumeSource{
                                Kind = "Managed",
                                DiskName = diskNewName,
                                DiskURI = newDiskID,
                            }
                        }
                    }
                }
            };
            await tkc.CreatePod(pod2);
            Assert.True(await tkc.WaitPodCompleted(pod2.Metadata.Name), "New pod construction failed");

            STEP("Validate disk data remain");
            var log = await tkc.ReadPodLog(pod2.Metadata.Name);
            Assert.True(log.Contains("temp"), "Cannot find the target file in the mount path");

            STEP("Validate dynamic delete the disk with reclaim \"delete\"");
            await tkc.ReplacePvReclaimPolicy(pvNewName, "Delete");
            await tkc.DeletePod(pod2.Metadata.Name);
            Assert.True(await azureDiskClient.ValidateDiskDeleted(diskNewName),"Fail to dynamic delete the azure disk");
        }

        private void STEP(string inf){
            logger.LogInformation("E2E TEST STEP: {0}", inf);
        }

        public async void Dispose()
        {
            STEP("Cleaning up");
            await tkc.DeleteNamespaceIfExists();
            this.tkc = null;
            this.azureDiskClient = null;
        }
    }
}
