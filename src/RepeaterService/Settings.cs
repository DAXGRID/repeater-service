namespace RepeaterService;

internal enum BusType
{
    RabbitMQ,
    AzureServiceBus
}

internal record Destination
{
    public string ConnectionString { get; init; }
    public BusType Type { get; init; }
    public List<(string header, string topic)> Topics { get; init; }

    public Destination(
        string connectionString,
        BusType type,
        List<(string header, string topic)> topics)
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
    List<Repeat> Repeats { get; init; }

    public Settings(List<Repeat> repeats)
    {
        if (repeats is null || repeats.Count == 0)
            throw new ArgumentException(
                "Cannot be null or empty", nameof(repeats));

        Repeats = repeats;
    }
}
