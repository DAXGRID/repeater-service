using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RepeaterService;

public class RepeaterServiceHost : BackgroundService
{
    private readonly ILogger<RepeaterServiceHost> _logger;

    public RepeaterServiceHost(
        ILogger<RepeaterServiceHost> logger)
    {
        _logger = logger;
    }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {nameof(RepeaterServiceHost)}.");
        await Task.CompletedTask;
    }
}
