using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RepeaterService;

internal class RepeaterServiceHost : BackgroundService
{
    private List<Repeater> _repeaters = new();
    private readonly ILogger _logger;

    public RepeaterServiceHost(ILoggerFactory loggerFactory, IOptions<Settings> settings)
    {
        _repeaters = settings.Value.Repeats.Select(x => new Repeater(x, loggerFactory)).ToList();
        _logger = loggerFactory.CreateLogger(nameof(RepeaterServiceHost));
    }

    protected async override Task ExecuteAsync(CancellationToken cToken)
    {
        _logger.LogInformation($"Starting {nameof(RepeaterServiceHost)}");
        foreach (var repeater in _repeaters)
            await repeater.Start().ConfigureAwait(false);
    }

    public override void Dispose()
    {
        foreach (var repeater in _repeaters)
            repeater.Dispose();

        base.Dispose();
    }
}
