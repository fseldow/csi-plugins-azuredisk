namespace Csi.Plugins.AzureDisk
{
    sealed class AzureResourceId
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string ProviderNamespace { get; set; }
        public string ResourceType { get; set; }
        public string Resource { get; set; }
        public string SubResourceType { get; set; }
        public string SubResource { get; set; }

        public override string ToString()
        {
            var resource = $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}" +
               $"/providers/{ProviderNamespace}/{ResourceType}/{Resource}";

            if (!string.IsNullOrEmpty(SubResourceType)) resource += $"/{SubResourceType}/{SubResource}";

            return resource;
        }

        public static AzureResourceId CreateForDisk(string subscription, string resourceGroup, string resource)
            => new AzureResourceId
            {
                ProviderNamespace = "Microsoft.Compute",
                ResourceType = "disks",
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                Resource = resource,
            };

        public static AzureResourceId CreateForVM(string subscription, string resourceGroup, string resource)
            => new AzureResourceId
            {
                ProviderNamespace = "Microsoft.Compute",
                ResourceType = "virtualMachines",
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                Resource = resource,
            };

        public static AzureResourceId CreateForVmssVm(string subscription, string resourceGroup, string resource, string instance)
           => new AzureResourceId
           {
               ProviderNamespace = "Microsoft.Compute",
               ResourceType = "virtualMachineScaleSets",
               Subscription = subscription,
               ResourceGroup = resourceGroup,
               Resource = resource,
               SubResourceType = "virtualMachines",
               SubResource = instance,
           };
    }
}
