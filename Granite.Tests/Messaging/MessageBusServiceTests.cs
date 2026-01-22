using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Services;
using Xunit;

namespace Granite.Tests.Messaging;

public class MessageBusServiceTests
{
    private readonly MessageBusService _sut;

    public MessageBusServiceTests()
    {
        _sut = new MessageBusService();
    }

    [Fact]
    public void Publish_WithValidMessage_BroadcastsToSubscribers()
    {
        var receivedMessages = new List<MessageBusMessage>();
        _sut.GetObservable().Subscribe(receivedMessages.Add);

        var message = new TestCommand { Data = new TestCommandSimpleData { Data = "test" } };
        _sut.Publish(message);

        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].Should().Be(message);
    }

    [Fact]
    public void Publish_WithNullMessage_DoesNotThrow()
    {
        var action = () => _sut.Publish(null!);
        action.Should().NotThrow();
    }

    [Fact]
    public void Publish_WithMultipleSubscribers_BroadcastsToAll()
    {
        var subscriber1Messages = new List<MessageBusMessage>();
        var subscriber2Messages = new List<MessageBusMessage>();

        _sut.GetObservable().Subscribe(subscriber1Messages.Add);
        _sut.GetObservable().Subscribe(subscriber2Messages.Add);

        var message = new TestCommand { Data = new TestCommandSimpleData { Data = "test" } };
        _sut.Publish(message);

        subscriber1Messages.Should().HaveCount(1);
        subscriber2Messages.Should().HaveCount(1);
        subscriber1Messages[0].Should().Be(message);
        subscriber2Messages[0].Should().Be(message);
    }

    [Fact]
    public void GetObservable_NewSubscriber_ReceivesBufferedEvents()
    {
        var message1 = new TestCommand { Data = new TestCommandSimpleData { Data = "test1" } };
        var message2 = new TestCommand { Data = new TestCommandSimpleData { Data = "test2" } };

        _sut.Publish(message1);
        _sut.Publish(message2);

        var receivedMessages = new List<MessageBusMessage>();
        _sut.GetObservable().Subscribe(receivedMessages.Add);

        receivedMessages.Should().HaveCount(2);
        receivedMessages[0].Should().Be(message1);
        receivedMessages[1].Should().Be(message2);
    }

    [Fact]
    public void Shutdown_CompletesObservable()
    {
        var completed = false;
        _sut.GetObservable().Subscribe(_ => { }, () => completed = true);

        _sut.Shutdown();

        completed.Should().BeTrue();
    }

    [Fact]
    public void Shutdown_PreventsNewPublishes()
    {
        _sut.Shutdown();

        var receivedMessages = new List<MessageBusMessage>();
        _sut.GetObservable().Subscribe(receivedMessages.Add);

        var message = new TestCommand { Data = new TestCommandSimpleData { Data = "test" } };
        _sut.Publish(message);

        receivedMessages.Should().BeEmpty();
    }

    [Fact]
    public void CreateCommand_SetsCorrectProperties()
    {
        var serverId = Guid.NewGuid();

        var command = _sut.CreateCommand<TestCommandWithTypedData>(serverId, cmd =>
        {
            cmd.Data.Name = "test data";
            cmd.Data.Value = 42;
        });

        command.Should().NotBeNull();
        command.TargetServerId.Should().Be(serverId);
        command.Data.Name.Should().Be("test data");
        command.Data.Value.Should().Be(42);
        command.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        command.TraceParent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateCommand_WithGenericData_InitializesDataProperty()
    {
        var serverId = Guid.NewGuid();

        var command = _sut.CreateCommand<TestCommandWithTypedData>(serverId, cmd =>
        {
            cmd.Data.Value = 42;
            cmd.Data.Name = "test";
        });

        command.Should().NotBeNull();
        command.Data.Should().NotBeNull();
        command.Data.Value.Should().Be(42);
        command.Data.Name.Should().Be("test");
    }

    [Fact]
    public void CreateEvent_SetsCorrectProperties()
    {
        var serverId = Guid.NewGuid();

        var @event = _sut.CreateEvent<TestEventWithTypedData>(serverId, evt =>
        {
            evt.Data.Id = Guid.NewGuid();
            evt.Data.Status = "test data";
        });

        @event.Should().NotBeNull();
        @event.OriginServerId.Should().Be(serverId);
        @event.Data.Status.Should().Be("test data");
        @event.Data.Id.Should().NotBeEmpty();
        @event.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.TraceParent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateEvent_WithGenericData_InitializesDataProperty()
    {
        var serverId = Guid.NewGuid();

        var @event = _sut.CreateEvent<TestEventWithTypedData>(serverId, evt =>
        {
            evt.Data.Id = Guid.NewGuid();
            evt.Data.Status = "active";
        });

        @event.Should().NotBeNull();
        @event.Data.Should().NotBeNull();
        @event.Data.Id.Should().NotBeEmpty();
        @event.Data.Status.Should().Be("active");
    }

    [Fact]
    public void Publish_MultipleMessages_MaintainsOrder()
    {
        var receivedMessages = new List<MessageBusMessage>();
        _sut.GetObservable().Subscribe(receivedMessages.Add);

        var messages = Enumerable.Range(1, 10)
            .Select(i => new TestCommand { Data = new TestCommandSimpleData { Data = $"test{i}" } })
            .ToList();

        foreach (var message in messages)
        {
            _sut.Publish(message);
        }

        receivedMessages.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            receivedMessages[i].Should().Be(messages[i]);
        }
    }

    [Fact]
    public void GetObservable_MultipleSubscriptions_AreIndependent()
    {
        var subscriber1Messages = new List<MessageBusMessage>();
        var subscriber2Messages = new List<MessageBusMessage>();

        var subscription1 = _sut.GetObservable().Subscribe(subscriber1Messages.Add);
        
        var message1 = new TestCommand { Data = new TestCommandSimpleData { Data = "test1" } };
        _sut.Publish(message1);

        subscription1.Dispose();

        var subscription2 = _sut.GetObservable().Subscribe(subscriber2Messages.Add);
        
        var message2 = new TestCommand { Data = new TestCommandSimpleData { Data = "test2" } };
        _sut.Publish(message2);

        subscriber1Messages.Should().HaveCount(1);
        subscriber2Messages.Should().HaveCount(2); // Gets buffered message1 + message2
    }
}

public class TestCommand : CommandMessage<TestCommandSimpleData> { }

public class TestCommandSimpleData
{
    public string Data { get; set; } = string.Empty;
}

public class TestCommandWithTypedData : CommandMessage<TestCommandData>
{
}

public class TestCommandData
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestEvent : EventMessage<TestEventSimpleData> { }

public class TestEventSimpleData
{
    public string Data { get; set; } = string.Empty;
}

public class TestEventWithTypedData : EventMessage<TestEventData>
{
}

public class TestEventData
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}
