namespace RepeaterService;

internal enum BusType
{
    RabbitMQ,
    AzureServiceBus
}


internal record DestinationTopic
{
    public string HeaderName { get; init; }
    public Dictionary<string, string> DestinationMaps { get; init; }

    public DestinationTopic(string headerName, Dictionary<string, string> destinationMaps)
    {
        HeaderName = headerName;
        DestinationMaps = destinationMaps;
    }
}

internal record Destination
{
    public string ConnectionString { get; init; }
    public BusType Type { get; init; }
    public DestinationTopic TopicMapping { get; init; }

    public Destination(
        string connectionString,
        BusType type,
        DestinationTopic topicMapping)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "Cannot be null or whitespace.", nameof(connectionString));
        ConnectionString = connectionString;
        Type = type;
        TopicMapping = topicMapping;
    }
}

internal record Subscription
{
    public string ConnectionString { get; init; }
    public BusType Type { get; init; }
    public List<string> Topics { get; init; }

    public Subscription(
        string connectionString,
        BusType type,
        List<string> topics)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "Cannot be null or whitespace.", nameof(connectionString));
        if (topics is null || topics.Count == 0)
            throw new ArgumentException(
                "Cannot be null or empty", nameof(topics));

        ConnectionString = connectionString;
        Type = type;
        Topics = topics;
    }
}

internal record Repeat(string Name, Subscription Subscription, Destination Destination);

internal record Settings
{
    public List<Repeat> Repeats { get; init; }

    public Settings(List<Repeat> repeats)
    {
        if (repeats is null || repeats.Count == 0)
            throw new ArgumentException(
                "Cannot be null or empty", nameof(repeats));

        Repeats = repeats;
    }
}
