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

    public RepeaterServiceHost(ILogger<RepeaterServiceHost> logger)
    {
        _logger = logger;

        var sub = new Subscription(
            "amqp://localhost",
            BusType.RabbitMQ,
            new() { "source_topic_one" });

        var dest = new Destination(
            "amqp://localhost",
            BusType.RabbitMQ,
            new("my_header_name", new() { { "*", "dest_topic_one" } }));

        var repeat = new Repeat("rabbit_to_rabbit", sub, dest);
        _settings = new(new() { repeat });
    }

    protected async override Task ExecuteAsync(CancellationToken cToken)
    {
        _logger.LogInformation($"Starting listening {nameof(RepeaterServiceHost)}.");
        var busses = _settings.Repeats.Select(r => BusPairFactory.Create(r)).ToList();

        foreach (var bus in busses)
            await bus.Source.Start().ConfigureAwait(false);

        var buss = busses.First();
        while (!cToken.IsCancellationRequested)
        {
            _ = cToken.WaitHandle.WaitOne(1000);
            // TODO use different types
            var message = new TestRecordOne("Rune", 28);
            await buss.Source.Publish(buss.Repeat.Subscription.Topics.First(), message)
                .ConfigureAwait(false);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
