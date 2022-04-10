using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace RepeaterService;

internal class RepeaterServiceHost : BackgroundService
{
    private List<Repeater> _repeaters = new();

    public RepeaterServiceHost(IOptions<Settings> settings)
    {
        _repeaters = settings.Value.Repeats.Select(x => new Repeater(x)).ToList();
    }

    protected async override Task ExecuteAsync(CancellationToken cToken)
    {
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
