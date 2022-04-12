using FluentAssertions;
using Rebus.Activation;
using Rebus.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RepeaterService.Tests;

public record TestMessageOne(string Content);
public record TestMessageTwo(string Content);

public class RepeaterServiceTests
{
    [Fact]
    public async Task Single_repeat_from_one_queue_to_another()
    {
        var sourceTopic = Guid.NewGuid().ToString();
        var destTopic = Guid.NewGuid().ToString();
        var rabbitConnectionString = "amqp://localhost";

        var sub = new Subscription
        {
            ConnectionString = rabbitConnectionString,
            Type = "RabbitMQ",
            Topic = sourceTopic,
            Name = "my_test_subscription",
            Create = true
        };

        var dest = new Destination
        {
            ConnectionString = rabbitConnectionString,
            Type = "RabbitMQ",
            TopicMapping = new()
            {
                HeaderName = "rbs2-msg-type",
                DestinationMaps = new()
                {
                    { "RepeaterService.Tests.TestMessageOne, RepeaterService.Tests", destTopic },
                    { "RepeaterService.Tests.TestMessageTwo, RepeaterService.Tests", destTopic }
                }
            }
        };

        var repeat = new RepeaterConfig
        {
            Name = "rabbit_to_rabbit",
            Subscription = sub,
            Destination = dest,
        };

        var settings = Microsoft.Extensions.Options.Options.Create(new Settings() { Repeats = new() { repeat } });
        var repeaterServiceHost = new RepeaterServiceHost(settings);
        await repeaterServiceHost.StartAsync(new());

        var activator = new BuiltinHandlerActivator();
        var testOneMessages = new List<TestMessageOne>();
        activator.Handle<TestMessageOne>(async x =>
        {
            testOneMessages.Add(x);
            await Task.CompletedTask;
        });

        var testTwoMessages = new List<TestMessageTwo>();
        activator.Handle<TestMessageTwo>(async x =>
        {
            testTwoMessages.Add(x);
            await Task.CompletedTask;
        });

        _ = Configure.With(activator)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => t.UseRabbitMq(rabbitConnectionString, "integration_test_rabbit_mq_queue"))
            .Start();

        await activator.Bus.Advanced.Topics.Subscribe(destTopic);

        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(200);
            if (i % 2 == 0)
                await activator.Bus.Advanced.Topics.Publish(
                    sourceTopic, new TestMessageOne($"This is a test message {i}"));
            else
                await activator.Bus.Advanced.Topics.Publish(
                    sourceTopic, new TestMessageTwo($"This is a test message {i}"));
        }

        testOneMessages.Should().HaveCount(5);
        testTwoMessages.Should().HaveCount(5);
    }
}
