using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Csi.Plugins.AzureDisk.Tests.Scenarios.K8s
{
    static class TestHelper
    {
        public static ILoggerFactory CreateLoggerFactory()
        {
            var logger = new LoggerConfiguration()
               .WriteTo.File("log.txt")
               .MinimumLevel.Debug()
               .CreateLogger();

            var lf= new LoggerFactory();
            lf.AddProvider(new SerilogLoggerProvider(logger));
            return lf;
        }
    }
}
