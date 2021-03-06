using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace RepeaterService.Config;

public static class HostConfig
{
    public static IHost Configure()
    {
        var hostBuilder = new HostBuilder();
      
        ConfigureApp(hostBuilder);
        ConfigureLogging(hostBuilder);
        ConfigureServices(hostBuilder);

        hostBuilder.UseWindowsService();

        return hostBuilder.Build();
    }

    private static void ConfigureApp(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(System.AppContext.BaseDirectory);
            config.AddJsonFile("appsettings.json", false, true);
        });
    }

    private static void ConfigureServices(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddOptions();
            services.AddHostedService<RepeaterServiceHost>();
            services.Configure<Settings>(s => hostContext.Configuration.GetSection("Settings").Bind(s));
        });
    }

    private static void ConfigureLogging(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            var loggingConfiguration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json")
                .Build();

            services.AddLogging(loggingBuilder =>
            {
                var logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(loggingConfiguration)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .CreateLogger();

                loggingBuilder.AddSerilog(logger, true);
            });
        });
    }
}
