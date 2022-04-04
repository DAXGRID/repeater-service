using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using System.Text;

namespace RepeaterService;

internal class AzureServiceBusListener : IDisposable
{
    private readonly BuiltinHandlerActivator _activator;

    public AzureServiceBusListener()
    {
        _activator = new();
    }

    public async Task Listen(Func<string, Task> handler)
    {
        _activator.Handle<String>(handler);

        // TODO for now listen to rabbitmq for testing purposes
        _ = Configure.With(_activator)
            .Logging(l => l.ColoredConsole(minLevel: Rebus.Logging.LogLevel.Info))
            .Transport(t => t.UseRabbitMq("amqp://localhost", "subscriber1"))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Options(o => o.Decorate<ISerializer>(c => new PlainJsonMessageSerializer(c.Get<ISerializer>())))
            .Start();

        await _activator.Bus.Advanced.Topics.Subscribe("my_topic_one");
    }

    public async Task Publish(string topic, string message)
    {
        await _activator.Bus.Advanced.Topics
            .Publish(topic, message).ConfigureAwait(false);
    }

    public void Dispose() => _activator.Dispose();
}

internal class PlainJsonMessageSerializer : ISerializer
{
    readonly ISerializer _serializer;

    public PlainJsonMessageSerializer(ISerializer serializer) =>
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public Task<TransportMessage> Serialize(Message message) => _serializer.Serialize(message);

    public async Task<Message> Deserialize(TransportMessage transportMessage)
    {
        var headers = transportMessage.Headers;
        var json = Encoding.UTF8.GetString(transportMessage.Body);
        return await Task.FromResult(new Message(headers, json)).ConfigureAwait(false);
    }
}
