using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    public static class ResourceIdExtensions
    {
        public static string LoggingId(this ResourceId resourceId)
        {
            return resourceId.ShortId();
        }

        public static string ShortId(this ResourceId resourceId)
            => resourceId.Parent != null
                ? $"{resourceId.ResourceGroupName}/{resourceId.Parent}/{resourceId.Name}"
                : $"{resourceId.ResourceGroupName}/{resourceId.Name}";

        public static IDisposable BeginResourceIdScope(this ILogger logger, string name, ResourceId resourceId)
            => logger.BeginKeyValueScope(name + "_id", resourceId.LoggingId());
    }
}
