using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.JsonPatch;

namespace Csi.Plugins.AzureDisk.Tests.Scenarios.K8s
{
    class TestKubernetesClient
    {
        private readonly string ns;
        private readonly Kubernetes client;
        private readonly ILogger logger;
        public TestKubernetesClient(string ns, ILogger<TestKubernetesClient> logger)
        {
            this.ns = ns;
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            client = new Kubernetes(config, new Handler(logger));
            this.logger = logger;
        }

        public async Task EnsureNamespace()
        {
            var a1 = await client.ListNamespaceAsync();
            logger.LogInformation("Creating namespace {0}", ns);
            foreach (var ns in a1.Items)
            {
                if(ns.Metadata.Name == this.ns){
                    logger.LogInformation("Namespace {0} already exists", ns);
                    return;
                }
            }
            var naso = new V1Namespace
            {
                Metadata = new V1ObjectMeta { Name = ns }
            };
            await client.CreateNamespaceAsync(naso);
        }

        public async Task DeleteNamespaceIfExists()
        {
            logger.LogInformation("Deleting namespace {0}", ns);
            var a1 = await client.ListNamespaceAsync();
            bool isFound = false;
            foreach (var ns in a1.Items)
            {
                if(ns.Metadata.Name == this.ns){
                    isFound = true;
                    break;
                }
            }
            if (!isFound){
                logger.LogInformation("Namespace {0} does not exist", ns);
                return;
            }
            var deleteOptions = new V1DeleteOptions();
            await client.DeleteNamespaceAsync(deleteOptions, ns);
        }

        public async Task<string> ReadPodLog(string podName)
        {  
            logger.LogInformation("Reading log of pod {0}", podName);
            var stre = await client.ReadNamespacedPodLogAsync(podName, ns);

            using (var ste = new StreamReader(stre))
            {
                var txt = await ste.ReadToEndAsync();
                return txt;
            }
        }

        public async Task CreatePvc(V1PersistentVolumeClaim pvc)
        {
            logger.LogInformation("Creating pvc {0} in namespace {1}", pvc.Metadata.Name, ns);
            await client.CreateNamespacedPersistentVolumeClaimAsync(pvc, ns);
        }

        public async Task DeletePvc(string pvcName){
            logger.LogInformation("Deleting pvc {0} in namespace {1}", pvcName, ns);
            var deleteOptions = new V1DeleteOptions();
            await client.DeleteNamespacedPersistentVolumeClaimAsync(deleteOptions, pvcName, ns);
        }

        public async Task CreatePod(V1Pod pod)
        {
            logger.LogInformation("Creating pod {0} in namespace {1}", pod.Metadata.Name, ns);
            await client.CreateNamespacedPodAsync(pod, ns);
        }

        public async Task<V1Pod> GetPod(string podName){
            return await client.ReadNamespacedPodAsync(podName, ns);
        }

        public async Task<bool> WaitPodCompleted(string podName){
            logger.LogInformation("waiting pod {0} completed", podName);
            var isCompleted = false;
            for (int i =0; i<= 10 *60; i+=5){
                Thread.Sleep(5*1000);
                var pod = await GetPod(podName);
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
                isCompleted = true;
                break;
            }
            return isCompleted;
        }

        public async Task DeletePod(string podName){
            logger.LogInformation("Deleting pod {0} in namespace {1}", podName, ns);
            var deleteOptions = new V1DeleteOptions();
            await client.DeleteNamespacedPodAsync(deleteOptions, podName, ns);
        }

        public async Task<V1PersistentVolumeList> GetPvList(){
            return await client.ListPersistentVolumeAsync();
        }

        public async Task ReplacePvReclaimPolicy(string pvName, string reclaimPolicy){
            logger.LogInformation("Changing the reclaim policy of persistent volume {0}, to {1}", pvName, reclaimPolicy);
            var pv = await client.ReadPersistentVolumeAsync(pvName);
            pv.Spec.PersistentVolumeReclaimPolicy = reclaimPolicy;

            await client.ReplacePersistentVolumeAsync(pv, pvName);
        } 
    }

    class Handler : DelegatingHandler
    {
        private readonly ILogger logger;
        public Handler(ILogger logger)
        {
            this.logger = logger;
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("response {0}", response.StatusCode);
                logger.LogInformation("response {0}", response.ReasonPhrase);
                foreach (var header in response.Headers)
                {
                    logger.LogDebug("{0}={1}", header.Key, header.Value);
                }

                var co = await response.Content.ReadAsStringAsync();
                logger.LogDebug("body: {0}", co);
            }
            return response;
        }
    }
}
