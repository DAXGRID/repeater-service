using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Transport;
using System.Text;

namespace RepeaterService;

internal class Repeater : IDisposable
{
    private readonly BuiltinHandlerActivator _activatorSource;
    private readonly BuiltinHandlerActivator _activatorDest;
    private readonly Repeat _repeat;

    public Repeater(Repeat repeat)
    {
        _activatorSource = new();
        _activatorDest = new();
        _repeat = repeat;
    }

    public async Task Start()
    {
        _activatorSource.Handle<String>(async (_, context, message) =>
        {
            var destTopic = string.Empty;
            if (_repeat.Destination.TopicMapping.HeaderName == "*")
            {
                destTopic = _repeat.Destination.TopicMapping.DestinationMaps["*"];
            }
            else
            {
                var headerValue = context.Headers[_repeat.Destination.TopicMapping.HeaderName];
                destTopic = _repeat.Destination.TopicMapping.DestinationMaps[headerValue];
            }

            Console.WriteLine("HEADER: " + String.Join(", ", context.Headers.Select(k => $"{k.Key} : {k.Value}")));
            await Publish(destTopic, message, context.Headers).ConfigureAwait(false);
        });

        // Setting up source
        _ = Configure.With(_activatorSource)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportType(t, "subscription"))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Options(o => o.Decorate<ISerializer>(c => new PlainJsonMessageSerializer(c.Get<ISerializer>())))
            .Start();

        // Setting up destination
        _ = Configure.With(_activatorDest)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportType(t, "destination"))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Options(o => o.Decorate<ISerializer>(c => new PlainJsonMessageSerializer(c.Get<ISerializer>())))
            .Start();

        foreach (var topic in _repeat.Subscription.Topics)
            await _activatorSource.Bus.Advanced.Topics.Subscribe(topic).ConfigureAwait(false);
    }

    private async Task Publish(string topic, object message, Dictionary<string, string> headers)
    {
        await _activatorDest.Bus.Advanced.Topics.Publish(topic, message, headers).ConfigureAwait(false);
    }

    public void Dispose() => _activatorSource.Dispose();

    private void SetupTransportType(StandardConfigurer<ITransport> t, string queueNameExtension)
    {
        switch (_repeat.Subscription.Type)
        {
            case BusType.RabbitMQ:
                t.UseRabbitMq(_repeat.Subscription.ConnectionString, $"{_repeat.Name}_{queueNameExtension}");
                break;
            case BusType.AzureServiceBus:
                t.UseAzureServiceBus(_repeat.Subscription.ConnectionString, $"{_repeat.Name}_{queueNameExtension}");
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
