namespace RepeaterService;

internal record DestinationTopic
{
    public string HeaderName { get; init; } = string.Empty;
    public Dictionary<string, string> DestinationMaps { get; init; } = new();
}

internal record Destination
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DestinationTopic TopicMapping { get; init; } = new();
}

internal record Subscription
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool Create { get; init; }
}

internal record RepeaterConfig
{
    public string Name { get; init; } = string.Empty;
    public Subscription Subscription { get; init; } = new();
    public Destination Destination { get; init; } = new();
}

internal record Settings
{
    public List<RepeaterConfig> Repeats { get; init; } = new();
}
