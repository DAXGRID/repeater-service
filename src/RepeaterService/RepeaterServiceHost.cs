using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RepeaterService;

record TestRecord(string Name, int Age);

public class RepeaterServiceHost : BackgroundService
{
    private readonly ILogger<RepeaterServiceHost> _logger;
    private readonly Bus _bus;

    public RepeaterServiceHost(ILogger<RepeaterServiceHost> logger)
    {
        _logger = logger;
        _bus = new();
    }

    protected async override Task ExecuteAsync(CancellationToken cToken)
    {
        var azureServiceHandler = async (string message) =>
        {
            _logger.LogInformation($"Got this message from bus: {message}");
            await Task.CompletedTask;
        };

        _logger.LogInformation($"Starting listening {nameof(RepeaterServiceHost)}.");
        await _bus.Start(azureServiceHandler).ConfigureAwait(false);

        while (!cToken.IsCancellationRequested)
        {
            _logger.LogInformation("Sending message.");
            var message = JsonConvert.SerializeObject(new TestRecord("Rune", 28));
            await _bus.Publish(
                "my_topic_one", message).ConfigureAwait(false);
            _ = cToken.WaitHandle.WaitOne(2000);
        }
    }

    public override void Dispose()
    {
        _bus.Dispose();
        base.Dispose();
    }
}
