using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RepeaterService;

record TestRecord(string Name, int Age);

public class RepeaterServiceHost : BackgroundService
{
    private readonly ILogger<RepeaterServiceHost> _logger;
    private readonly AzureServiceBusListener _azureServiceBusListener;

    public RepeaterServiceHost(ILogger<RepeaterServiceHost> logger)
    {
        _logger = logger;
        _azureServiceBusListener = new();
    }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var azureServiceBusHandler = async (string message) =>
        {
            _logger.LogInformation($"Got this message from AZB: {message}");
            await Task.CompletedTask;
        };

        _logger.LogInformation($"Starting listening {nameof(RepeaterServiceHost)}.");
        await _azureServiceBusListener.Listen(azureServiceBusHandler).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Sending message.");
            var message = JsonConvert.SerializeObject(new TestRecord("Rune", 28));
            await _azureServiceBusListener.Publish("my_topic_one", message).ConfigureAwait(false);
            _ = cancellationToken.WaitHandle.WaitOne(2000);
        }
    }

    public override void Dispose()
    {
        _azureServiceBusListener.Dispose();
        base.Dispose();
    }
}
