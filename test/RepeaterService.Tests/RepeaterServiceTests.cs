using Rebus.Activation;
using Rebus.Config;
using System;
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
        var sub = new Subscription(
           "amqp://localhost",
           BusType.RabbitMQ,
           new() { "source_topic_one" });

        var dest = new Destination(
            "amqp://localhost",
            BusType.RabbitMQ,
            new("rbs2-msg-type", new()
            {
                { "RepeaterService.Tests.TestMessageOne, RepeaterService.Tests", "dest_topic_one" },
                { "RepeaterService.Tests.TestMessageTwo, RepeaterService.Tests", "dest_topic_one" }
            }));

        var repeat = new RepeaterConfig("rabbit_to_rabbit", sub, dest);
        var settings = new Settings(new() { repeat });

        var repeaterServiceHost = new RepeaterServiceHost(settings);
        await repeaterServiceHost.StartAsync(new());

        var testRabbit = GetTestRabbitMQ();
        await testRabbit.Bus.Advanced.Topics.Subscribe("dest_topic_one");

        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(2000);

            if (i % 2 == 0)
            {
                await testRabbit.Bus.Advanced.Topics.Publish(
                    "source_topic_one", new TestMessageOne($"This is a test message {i}"));
            }
            else
            {
                await testRabbit.Bus.Advanced.Topics.Publish(
                    "source_topic_one", new TestMessageTwo($"This is a test message {i}"));
            }
        }
    }

    public BuiltinHandlerActivator GetTestRabbitMQ()
    {
        var activator = new BuiltinHandlerActivator();

        activator.Handle<TestMessageOne>(async x =>
        {
            Console.WriteLine("Got TestMessageOne message with content: " + x.Content);
            await Task.CompletedTask;
        });

        activator.Handle<TestMessageTwo>(async x =>
        {
            Console.WriteLine("Got TestMessageTwo message with content: " + x.Content);
            await Task.CompletedTask;
        });

        // Setting up source
        _ = Configure.With(activator)
            .Logging(l => l.Console(minLevel: Rebus.Logging.LogLevel.Warn))
            .Transport(t => t.UseRabbitMq("amqp://localhost", "integration_test_rabbit_mq_queue"))
            .Start();

        return activator;
    }
}
