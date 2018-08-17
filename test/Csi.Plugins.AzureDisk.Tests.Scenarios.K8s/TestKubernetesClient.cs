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
            foreach (var ns in a1.Items)
            {
                logger.LogInformation("{0}", ns.Metadata.Name);
            }
            logger.LogInformation("{0}", a1);
            var naso = new V1Namespace
            {
                Metadata = new V1ObjectMeta { Name = ns }
            };
            await client.CreateNamespaceAsync(naso);
        }

        public async Task<string> ReadPodLog(string podName)
        {
            var stre = await client.ReadNamespacedPodLogAsync(podName, ns);

            using (var ste = new StreamReader(stre))
            {
                var txt = await ste.ReadToEndAsync();
                return txt;
            }
        }

        public async Task CreatePvc(V1PersistentVolumeClaim pvc)
        {
            await client.CreateNamespacedPersistentVolumeClaimAsync(pvc, ns);
        }

        public async Task DeletePvc(string pvcName){
            var deleteOptions = new V1DeleteOptions();
            await client.DeleteNamespacedPersistentVolumeClaimAsync(deleteOptions, pvcName, ns);
        }

        public async Task CreatePod(V1Pod pod)
        {
            await client.CreateNamespacedPodAsync(pod, ns);
        }

        public async Task<V1Pod> GetPod(string podName){
            return await client.ReadNamespacedPodAsync(podName, ns);
        }

        public async Task PatchPod(){
            var pod = client.ListNamespacedPod("default").Items[0];    
            var newlables = new Dictionary<string, string>
            {
                ["testf"] = "test"
            };
            var patch = new JsonPatchDocument<V1Pod>();
            patch.Replace(e => e.Metadata.Labels, newlables);
            await client.PatchNamespacedPodAsync(new V1Patch(patch), pod.Metadata.Name, "default");
        }

        public async Task DeletePod(string podName){
            var deleteOptions = new V1DeleteOptions();
            await client.DeleteNamespacedPodAsync(deleteOptions, podName, ns);
        }

        public async Task<V1PersistentVolumeList> GetPvList(){
            return await client.ListPersistentVolumeAsync();
        }

        public async Task PatchPvReclaimPolicy(string pvName, string reclaimPolicy){
            var patch = new JsonPatchDocument<V1PersistentVolume>();
            patch.Replace(e => e.Spec.PersistentVolumeReclaimPolicy, "Reclaim");

            await client.PatchPersistentVolumeAsync(new V1Patch(patch), pvName);
        }

        public async Task DeleteNamespace()
        {
            var deleteOptions = new V1DeleteOptions();
            await client.DeleteNamespaceAsync(deleteOptions, ns);
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
