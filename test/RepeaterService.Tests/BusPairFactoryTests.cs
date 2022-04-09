using FluentAssertions;
using Xunit;

namespace RepeaterService.Tests;

public class BusPairFactoryTests
{
    [Fact]
    public void Create()
    {
        var sub = new Subscription(
            "amqp://localhost",
            BusType.RabbitMQ,
            new() { "my-topic-one", "my-topic-two" });

        var dest = new Destination(
            "amqp://localhost",
            BusType.RabbitMQ,
            new() { ("*", "topic-one") });

        var repeat = new Repeat("rabbit_to_rabbit", sub, dest);

        var busPair = BusPairFactory.Create(repeat);

        busPair.Repeat.Should().Be(repeat);
        busPair.Source.Should().NotBeNull();
        busPair.Destination.Should().NotBeNull();
    }
}
