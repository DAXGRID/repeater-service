using Microsoft.Extensions.Hosting;

namespace RepeaterService;

internal class RepeaterServiceHost : BackgroundService
{
    private List<Repeater> _repeaters = new();

    public RepeaterServiceHost(Settings settings)
    {
        _repeaters = settings.Repeats.Select(x => new Repeater(x)).ToList();
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
