using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RepeaterService;

record TestRecordOne(string Name, int Age);
record TestRecordTwo(string Name, int Age);
record TestRecordThree(string Name, int Age);

internal class RepeaterServiceHost : BackgroundService
{
    private readonly ILogger<RepeaterServiceHost> _logger;
    private readonly Settings _settings;
    private List<Repeater> _repeaters = new();

    public RepeaterServiceHost(ILogger<RepeaterServiceHost> logger)
    {
        _logger = logger;

        // TODO move this to settings
        var sub = new Subscription(
            "amqp://localhost",
            BusType.RabbitMQ,
            new() { "source_topic_one" });

        var dest = new Destination(
            "amqp://localhost",
            BusType.RabbitMQ,
            new("rbs2-msg-type", new() { { "RepeaterService.TestRecordOne, RepeaterService", "dest_topic_one" } }));

        var repeat = new RepeaterConfig("rabbit_to_rabbit", sub, dest);
        _settings = new(new() { repeat });

        _repeaters = _settings.Repeats.Select(x => new Repeater(x)).ToList();
    }

    protected async override Task ExecuteAsync(CancellationToken cToken)
    {
        _logger.LogInformation($"Starting {nameof(RepeaterServiceHost)}.");
        foreach (var repeater in _repeaters)
            await repeater.Start().ConfigureAwait(false);
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
