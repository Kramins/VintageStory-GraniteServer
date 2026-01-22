using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Handlers.Commands;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Messaging.Handlers;

public class CommandHandlerTests
{
    [Fact]
    public async Task ICommandHandler_Handle_CallsTypedHandle()
    {
        var handler = new TestCommandHandler();
        var command = new TestCommand { Data = new TestCommandData { Data = "test" } };

        await ((ICommandHandler)handler).Handle(command);

        handler.ExecutedCommands.Should().ContainSingle();
        handler.ExecutedCommands[0].Data.Data.Should().Be("test");
    }

    [Fact]
    public async Task ConcreteCommandHandler_HandlesCommand()
    {
        var handler = new TestCommandHandler();
        var command = new TestCommand { Data = new TestCommandData { Data = "execute" } };

        await handler.Handle(command);

        handler.ExecutedCommands.Should().ContainSingle();
        handler.ExecutedCommands[0].Should().Be(command);
    }

    [Fact]
    public async Task ConcreteCommandHandler_ThroughInterface_HandlesCommand()
    {
        ICommandHandler handler = new TestCommandHandler();
        var command = new TestCommand { Data = new TestCommandData { Data = "execute" } };

        await handler.Handle(command);

        var concreteHandler = (TestCommandHandler)handler;
        concreteHandler.ExecutedCommands.Should().ContainSingle();
    }

    [Fact]
    public async Task MultipleHandlers_HandleDifferentCommands()
    {
        var testHandler = new TestCommandHandler();
        var anotherHandler = new AnotherCommandHandler();

        var testCommand = new TestCommand { Data = new TestCommandData { Data = "test" } };
        var anotherCommand = new AnotherCommand { Data = 42 };

        await testHandler.Handle(testCommand);
        await anotherHandler.Handle(anotherCommand);

        testHandler.ExecutedCommands.Should().ContainSingle();
        anotherHandler.ExecutedCommands.Should().ContainSingle();
    }
}

public class TestCommand : CommandMessage<TestCommandData> { }

public class TestCommandData
{
    public string Data { get; set; } = string.Empty;
}

public class AnotherCommand : CommandMessage<int> { }

public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public List<TestCommand> ExecutedCommands { get; } = new();

    public Task Handle(TestCommand command)
    {
        ExecutedCommands.Add(command);
        return Task.CompletedTask;
    }
}

public class AnotherCommandHandler : ICommandHandler<AnotherCommand>
{
    public List<AnotherCommand> ExecutedCommands { get; } = new();

    public Task Handle(AnotherCommand command)
    {
        ExecutedCommands.Add(command);
        return Task.CompletedTask;
    }
}
