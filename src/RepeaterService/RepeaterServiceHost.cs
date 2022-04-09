using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RepeaterService;

record TestRecord(string Name, int Age);

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
            new() { ("*", "destination_topic_one") });

        var repeat = new Repeat("rabbit_to_rabbit", sub, dest);
        _settings = new(new() { repeat });
    }

    protected async override Task ExecuteAsync(CancellationToken cToken)
    {
        _logger.LogInformation($"Starting listening {nameof(RepeaterServiceHost)}.");
        var busses = _settings.Repeats.Select(r => BusPairFactory.Create(r)).ToList();

        foreach (var bus in busses)
        {
            var handler = async (string message) =>
            {
                _logger.LogInformation($"Got this message from bus: {message}");
                //await bus.Destination.Publish(bus.Repeat.Destination.Topics.First().topic, message);
                await Task.CompletedTask;
            };

            await bus.Source.Start(handler).ConfigureAwait(false);
        }

        var buss = busses.First();
        while (!cToken.IsCancellationRequested)
        {
            _ = cToken.WaitHandle.WaitOne(1000);
            var message = JsonConvert.SerializeObject(new TestRecord("Rune", 28));
            await buss.Source.Publish(buss.Repeat.Subscription.Topics.First(), message).ConfigureAwait(false);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
