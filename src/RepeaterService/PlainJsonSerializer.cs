using Rebus.Messages;
using Rebus.Serialization;
using System.Text;

namespace RepeaterService;

internal class PlainJsonMessageSerializer : ISerializer
{
    readonly ISerializer _serializer;

    public PlainJsonMessageSerializer(ISerializer serializer)
        => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public Task<TransportMessage> Serialize(Message message)
        => _serializer.Serialize(message);

    public async Task<Message> Deserialize(TransportMessage transportMessage)
        => await Task.FromResult(new Message(transportMessage.Headers, Encoding.UTF8.GetString(transportMessage.Body))).ConfigureAwait(false);
}
