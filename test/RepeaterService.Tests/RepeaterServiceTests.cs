using Rebus.Activation;
using Rebus.Config;
using Rebus.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RepeaterService.Tests;

public class TestMessageOne
{
    public string Content { get; set; }

    public TestMessageOne(string content)
    {
        Content = content;
    }
}

public class RepeaterServiceTests
{
    [Fact]
    public async Task Single_repeat_from_one_queue_to_another()
    {
        var sub = new Subscription(
           "amqp://localhost",
           BusType.RabbitMQ,
           new() { "source_topic_one" });

        var dest = new Destination(
            "amqp://localhost",
            BusType.RabbitMQ,
            new("rbs2-msg-type", new()
            {
                { "RepeaterService.Tests.TestMessageOne, RepeaterService.Tests", "dest_topic_one" }
            }));

        var repeat = new RepeaterConfig("rabbit_to_rabbit", sub, dest);
        var settings = new Settings(new() { repeat });

        var repeaterServiceHost = new RepeaterServiceHost(settings);
        await repeaterServiceHost.StartAsync(new());

        var handler = async (TestMessageOne x) =>
        {
            Console.WriteLine("Got message with content: " + x.Content);
            await Task.CompletedTask;
        };

        var testRabbit = GetTestRabbitMQ(new() { handler });
        await testRabbit.Bus.Advanced.Topics.Subscribe("dest_topic_one");

        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(2000);
            await testRabbit.Bus.Advanced.Topics.Publish(
                "source_topic_one", new TestMessageOne($"This is a test message {i}"));
        }
    }

    public BuiltinHandlerActivator GetTestRabbitMQ(List<Func<TestMessageOne, Task>> handlers)
    {
        var activator = new BuiltinHandlerActivator();
        foreach (var handler in handlers)
            activator.Handle(handler);

        // Setting up source
        _ = Configure.With(activator)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => t.UseRabbitMq("amqp://localhost", "integration_test_rabbit_mq_queue"))
            .Serialization(s => s.UseNewtonsoftJson())
            .Start();

        return activator;
    }
}
