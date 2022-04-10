using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Transport;
using System.Text;

namespace RepeaterService;

internal class Bus : IDisposable
{
    private readonly BuiltinHandlerActivator _activator;
    private readonly Repeat _repeat;

    public Bus(Repeat repeat)
    {
        _activator = new();
        _repeat = repeat;
    }

    public async Task Start()
    {
        _activator.Handle<String>(async (_, x, _) =>
        {
            Console.WriteLine("HEADER: " + String.Join(", ", x.Headers.Select(k => $"{k.Key} : {k.Value}")));
            await Task.CompletedTask;
        });

        _ = Configure.With(_activator)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportType(t))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Options(o => o.Decorate<ISerializer>(c => new PlainJsonMessageSerializer(c.Get<ISerializer>())))
            .Start();

        foreach (var topic in _repeat.Subscription.Topics)
            await _activator.Bus.Advanced.Topics.Subscribe(topic).ConfigureAwait(false);
    }

    public async Task Publish(string topic, object message)
    {
        await _activator.Bus.Advanced.Topics.Publish(topic, message).ConfigureAwait(false);
    }

    public void Dispose() => _activator.Dispose();

    private void SetupTransportType(StandardConfigurer<ITransport> t)
    {
        switch (_repeat.Subscription.Type)
        {
            case BusType.RabbitMQ:
                t.UseRabbitMq(_repeat.Subscription.ConnectionString, $"{_repeat.Name}_subscription");
                break;
            case BusType.AzureServiceBus:
                t.UseAzureServiceBus(_repeat.Subscription.ConnectionString, $"{_repeat.Name}_subscription");
                break;
            default:
                throw new ArgumentException($"{_repeat.Subscription.Type} is not valid.", nameof(_repeat.Subscription.Type));
        }
    }

    private class PlainJsonMessageSerializer : ISerializer
    {
        readonly ISerializer _serializer;

        public PlainJsonMessageSerializer(ISerializer serializer)
            => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

        public Task<TransportMessage> Serialize(Message message)
            => _serializer.Serialize(message);

        public async Task<Message> Deserialize(TransportMessage transportMessage)
            => await Task.FromResult(new Message(transportMessage.Headers, Encoding.UTF8.GetString(transportMessage.Body))).ConfigureAwait(false);
    }
}
