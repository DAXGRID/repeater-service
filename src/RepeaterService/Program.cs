using Microsoft.Extensions.Hosting;
using RepeaterService.Config;

namespace RepeaterService;

internal static class Program
{
    internal static async Task Main()
    {
        using (var host = HostConfig.Configure())
        {
            await host.StartAsync();
            await host.WaitForShutdownAsync();
        }
    }
}
