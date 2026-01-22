using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Messaging.Handlers;

public class EventHandlerTests
{
    [Fact]
    public async Task IEventHandler_Handle_CallsTypedHandle()
    {
        var handler = new TestEventHandler();
        var @event = new TestEvent { Data = new TestEventData { Data = "test" } };

        await ((IEventHandler)handler).Handle(@event);

        handler.HandledEvents.Should().ContainSingle();
        handler.HandledEvents[0].Data.Data.Should().Be("test");
    }

    [Fact]
    public async Task ConcreteEventHandler_HandlesEvent()
    {
        var handler = new TestEventHandler();
        var @event = new TestEvent { Data = new TestEventData { Data = "execute" } };

        await handler.Handle(@event);

        handler.HandledEvents.Should().ContainSingle();
        handler.HandledEvents[0].Should().Be(@event);
    }

    [Fact]
    public async Task ConcreteEventHandler_ThroughInterface_HandlesEvent()
    {
        IEventHandler handler = new TestEventHandler();
        var @event = new TestEvent { Data = new TestEventData { Data = "execute" } };

        await handler.Handle(@event);

        var concreteHandler = (TestEventHandler)handler;
        concreteHandler.HandledEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task MultipleHandlers_HandleDifferentEvents()
    {
        var testHandler = new TestEventHandler();
        var anotherHandler = new AnotherEventHandler();

        var testEvent = new TestEvent { Data = new TestEventData { Data = "test" } };
        var anotherEvent = new AnotherEvent { Data = 100 };

        await testHandler.Handle(testEvent);
        await anotherHandler.Handle(anotherEvent);

        testHandler.HandledEvents.Should().ContainSingle();
        anotherHandler.HandledEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task EventHandler_HandlesMultipleEvents()
    {
        var handler = new TestEventHandler();

        var event1 = new TestEvent { Data = new TestEventData { Data = "first" } };
        var event2 = new TestEvent { Data = new TestEventData { Data = "second" } };
        var event3 = new TestEvent { Data = new TestEventData { Data = "third" } };

        await handler.Handle(event1);
        await handler.Handle(event2);
        await handler.Handle(event3);

        handler.HandledEvents.Should().HaveCount(3);
        handler.HandledEvents[0].Data.Data.Should().Be("first");
        handler.HandledEvents[1].Data.Data.Should().Be("second");
        handler.HandledEvents[2].Data.Data.Should().Be("third");
    }
}

public class TestEvent : EventMessage<TestEventData> { }

public class TestEventData
{
    public string Data { get; set; } = string.Empty;
}

public class AnotherEvent : EventMessage<int> { }

public class TestEventHandler : IEventHandler<TestEvent>
{
    public List<TestEvent> HandledEvents { get; } = new();

    public Task Handle(TestEvent command)
    {
        HandledEvents.Add(command);
        return Task.CompletedTask;
    }
}

public class AnotherEventHandler : IEventHandler<AnotherEvent>
{
    public List<AnotherEvent> HandledEvents { get; } = new();

    public Task Handle(AnotherEvent command)
    {
        HandledEvents.Add(command);
        return Task.CompletedTask;
    }
}
