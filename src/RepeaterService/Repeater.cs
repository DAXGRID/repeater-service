using Newtonsoft.Json.Linq;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Transport;
using System;
using System.Text;

namespace RepeaterService;

internal class Repeater : IDisposable
{
    private readonly BuiltinHandlerActivator _activatorSource;
    private readonly BuiltinHandlerActivator _activatorDest;
    private readonly RepeaterConfig _repeat;

    public Repeater(RepeaterConfig repeat)
    {
        _activatorSource = new();
        _activatorDest = new();
        _repeat = repeat;
    }

    public async Task Start()
    {
        _activatorSource.Handle<string>(async (_, context, message) =>
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

            await Publish(destTopic, JObject.Parse(message), context.Headers).ConfigureAwait(false);
        });

        // Setting up source
        _ = Configure.With(_activatorSource)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportSubscription(t))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Options(o => o.Decorate<ISerializer>(c => new PlainJsonMessageSerializer(c.Get<ISerializer>())))
            .Start();

        // Setting up destination
        _ = Configure.With(_activatorDest)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportDestination(t))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Start();

        foreach (var topic in _repeat.Subscription.Topics)
            await _activatorSource.Bus.Advanced.Topics.Subscribe(topic).ConfigureAwait(false);
    }

    private async Task Publish(string topic, JObject message, Dictionary<string, string> headers)
    {
        await _activatorDest.Bus.Advanced.Topics.Publish(topic, message, headers).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _activatorSource.Dispose();
        _activatorDest.Dispose();
    }

    private void SetupTransportSubscription(StandardConfigurer<ITransport> t)
    {
        switch (_repeat.Subscription.Type)
        {
            case "RabbitMQ":
                t.UseRabbitMq(_repeat.Subscription.ConnectionString, _repeat.Subscription.Name);
                break;
            case "AzureServiceBus":
                t.UseAzureServiceBus(_repeat.Subscription.ConnectionString, _repeat.Subscription.Name);
                break;
            default:
                throw new ArgumentException($"{_repeat.Subscription.Type} is not valid.", nameof(_repeat.Subscription.Type));
        }
    }

    private void SetupTransportDestination(StandardConfigurer<ITransport> t)
    {
        switch (_repeat.Destination.Type)
        {
            case "RabbitMQ":
                t.UseRabbitMqAsOneWayClient(_repeat.Destination.ConnectionString);
                break;
            case "AzureServiceBus":
                t.UseAzureServiceBusAsOneWayClient(_repeat.Destination.ConnectionString);
                break;
            default:
                throw new ArgumentException($"{_repeat.Destination} is not valid.", nameof(_repeat.Destination.Type));
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
