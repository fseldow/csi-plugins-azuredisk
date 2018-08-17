using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading;


namespace Csi.Plugins.AzureDisk.Tests.Scenarios.K8s
{
    public class BasicTest
    {
        private static readonly ILoggerFactory loggerFactory = TestHelper.CreateLoggerFactory();

        [Fact]
        public async Task PodWithPvc()
        {

            var testNamespace = "e2e-tests-csi-disk" + Guid.NewGuid().ToString(); ;
            var tkc = new TestKubernetesClient(testNamespace, loggerFactory.CreateLogger<TestKubernetesClient>());
            //await tkc.PatchPod();
            await tkc.PatchPvReclaimPolicy("pvc-80e5e8e1a13c11e8", "Retain");

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
            var azureDiskClient = new TestAzureDisk();
            //await azureDiskClient.CreateDisk("t-xinhli-11-vm-11", "abcdefg");
            await tkc.EnsureNamespace();
            var pvListOld = await tkc.GetPvList();
            await tkc.CreatePvc(pvc);
            await tkc.CreatePod(pod);

            // TODO verify pod status, share content, and do clean up

            // Fetch the name of the new share with retry
            for (int i =0; i<= 10 *60; i+=5){
                Thread.Sleep(5*1000);
                pod = await tkc.GetPod(pod.Metadata.Name);
                var status = pod.Status;
                if(status.Phase != "Succeeded"){
                    continue;
                }
                if(status.ContainerStatuses[0].State.Terminated == null){
                    continue;
                }
                if(status.ContainerStatuses[0].State.Terminated.Reason!="Completed"){
                    continue;
                }
                break;
            }

            var pvListNew = await tkc.GetPvList();
            string pvNewName = "";
            foreach(var pvNew in pvListNew.Items){
                bool isFind = false;
                foreach(var pvOld in pvListOld.Items){
                    if(pvOld.Metadata.Name == pvNew.Metadata.Name){
                        isFind = true;
                        break;
                    }
                }
                if(!isFind){
                    pvNewName = pvNew.Metadata.Name;
                    break;
                }
            }

            await tkc.PatchPvReclaimPolicy(pvNewName, "Retain");
            await tkc.DeletePod(pod.Metadata.Name);
            await tkc.DeletePvc(pvcName);


            var pvcName2 = "pvc2";
            var pvc2 = new V1PersistentVolumeClaim
            {
                Metadata = new V1ObjectMeta
                {
                    Name = pvcName2,
                },
                Spec = new V1PersistentVolumeClaimSpec
                {
                    VolumeName = pvNewName,
                },
            };
            pod.Spec.Containers[0].Args = new List<string>{"ls", "/af-vol"};
            await tkc.CreatePvc(pvc2);
            await tkc.CreatePod(pod);
            for (int i =0; i<= 10 *60; i+=5){
                Thread.Sleep(5*1000);
                pod = await tkc.GetPod(pod.Metadata.Name);
                var status = pod.Status;
                if(status.Phase != "Succeeded"){
                    continue;
                }
                if(status.ContainerStatuses[0].State.Terminated == null){
                    continue;
                }
                if(status.ContainerStatuses[0].State.Terminated.Reason!="Completed"){
                    continue;
                }
                break;
            }
            var log = await tkc.ReadPodLog(pod.Metadata.Name);
            Assert.Contains(log, "temp");

            await tkc.DeleteNamespace();
        }
    }
}
