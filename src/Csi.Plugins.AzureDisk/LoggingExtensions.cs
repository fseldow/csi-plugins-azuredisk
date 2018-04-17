using Microsoft.Extensions.Logging;
using Serilog;

namespace Csi.Plugins.AzureDisk
{
    static class LoggingExtensions {
        // {SourceContext}
        private const string template = "{Timestamp:mm:ss}[{Level:u1}] {Scope:l}{Message:lj}{NewLine}{Exception}";
        public static ILoggingBuilder AddSerillogConsole(this ILoggingBuilder lb){
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: template)
                .CreateLogger();
            return lb.AddSerilog(logger);
        }
    }
}
