using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Routing.TransportMessages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Transport;
using System.Text;

namespace RepeaterService;

internal class Repeater : IDisposable
{
    private readonly BuiltinHandlerActivator _activatorSource;
    private readonly BuiltinHandlerActivator _activatorDest;
    private readonly RepeaterConfig _repeat;
    private readonly ILogger _logger;

    public Repeater(RepeaterConfig repeat, ILoggerFactory loggerFactory)
    {
        _activatorSource = new();
        _activatorDest = new();
        _repeat = repeat;
        _logger = loggerFactory.CreateLogger(nameof(Repeater));
    }

    public async Task Start()
    {
        var handler = async (TransportMessage message) =>
        {
            _logger.LogInformation("Received message.");
            var destTopic = string.Empty;
            if (_repeat.Destination.TopicMapping.HeaderName == "*")
            {
                destTopic = _repeat.Destination.TopicMapping.DestinationMaps["*"];
            }
            else
            {
                var headerValue = message.Headers[_repeat.Destination.TopicMapping.HeaderName];
                destTopic = _repeat.Destination.TopicMapping.DestinationMaps[headerValue];
            }

            var messageBody = Encoding.UTF8.GetString(message.Body);
            await Publish(destTopic, JObject.Parse(messageBody), message.Headers).ConfigureAwait(false);
        };

        // Setting up source
        _ = Configure.With(_activatorSource)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportSubscription(t))
            .Routing(r => r.AddTransportMessageForwarder(async tm =>
            {
                await handler(tm);
                return ForwardAction.Ignore();
            }))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Options(o => o.Decorate<ISerializer>(c => new PlainJsonMessageSerializer(c.Get<ISerializer>())))
            .Start();

        // Setting up destination
        _ = Configure.With(_activatorDest)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => SetupTransportDestination(t))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Start();

        if (_repeat.Subscription.Create)
            await _activatorSource.Bus.Advanced.Topics.Subscribe(_repeat.Subscription.Topic).ConfigureAwait(false);
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
                if (_repeat.Subscription.Create)
                    t.UseAzureServiceBus(_repeat.Subscription.ConnectionString, _repeat.Subscription.Name);
                else
                    t.UseAzureServiceBus(_repeat.Subscription.ConnectionString, _repeat.Subscription.Name)
                        .DoNotCreateQueues()
                        .DoNotCheckQueueConfiguration();
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
