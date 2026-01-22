using System;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Messaging.Events;
using Granite.Server.Handlers.Events;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using GraniteServer.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Granite.Tests.Handlers;

public class ServerMetricsEventHandlerTests
{
    private readonly GraniteDataContext _mockDataContext;
    private readonly ILogger<ServerMetricsEventHandler> _mockLogger;
    private readonly ServerMetricsEventHandler _handler;

    public ServerMetricsEventHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"ServerMetricsTests_{Guid.NewGuid()}")
            .Options;

        _mockDataContext = new GraniteDataContext(options);
        _mockLogger = Substitute.For<ILogger<ServerMetricsEventHandler>>();
        _handler = new ServerMetricsEventHandler(_mockDataContext, _mockLogger);
    }

    [Fact]
    public async Task Handle_ValidMetricsEvent_SavesMetricsToDatabase()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var @event = new ServerMetricsEvent
        {
            OriginServerId = serverId,
            Data = new ServerMetricsEventData
            {
                CpuUsagePercent = 45.5f,
                MemoryUsageMB = 512.75f,
                ActivePlayerCount = 10,
            },
        };

        // Act
        await ((IEventHandler<ServerMetricsEvent>)_handler).Handle(@event);

        // Assert
        var saved = await _mockDataContext.ServerMetrics.SingleAsync(m => m.ServerId == serverId);
        saved.CpuUsagePercent.Should().Be(45.5f);
        saved.MemoryUsageMB.Should().Be(512.75f);
        saved.ActivePlayerCount.Should().Be(10);
    }

    [Fact]
    public async Task Handle_NullEventData_LogsWarningAndReturnsEarly()
    {
        // Arrange
        var @event = new ServerMetricsEvent { OriginServerId = Guid.NewGuid() };
        ((MessageBusMessage)@event).Data = null;

        // Act
        await ((IEventHandler<ServerMetricsEvent>)_handler).Handle(@event);

        // Assert
        _mockLogger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("null data")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );

        (await _mockDataContext.ServerMetrics.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_SaveChangesThrowsException_CatchesAndLogsError()
    {
        // Arrange
        var @event = new ServerMetricsEvent
        {
            OriginServerId = Guid.NewGuid(),
            Data = new ServerMetricsEventData
            {
                CpuUsagePercent = 50f,
                MemoryUsageMB = 1024f,
                ActivePlayerCount = 5,
            },
        };

        var saveException = new InvalidOperationException("Database error");

        // Use a context that throws on SaveChangesAsync to simulate a DB error
        var throwingOptions = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"ServerMetricsTests_Throw_{Guid.NewGuid()}")
            .Options;

        var throwingContext = new ThrowingGraniteDataContext(throwingOptions, saveException);
        var throwingHandler = new ServerMetricsEventHandler(throwingContext, _mockLogger);

        // Act
        await ((IEventHandler<ServerMetricsEvent>)throwingHandler).Handle(@event);

        // Assert
        _mockLogger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Error persisting")),
                Arg.Is<Exception>(e => e == saveException),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    private class ThrowingGraniteDataContext : GraniteDataContext
    {
        private readonly Exception _exceptionToThrow;

        public ThrowingGraniteDataContext(DbContextOptions options, Exception exceptionToThrow)
            : base(options)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw _exceptionToThrow;
        }
    }

    [Fact]
    public async Task Handle_EdgeValuesMetrics_PersistsCorrectly()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var @event = new ServerMetricsEvent
        {
            OriginServerId = serverId,
            Data = new ServerMetricsEventData
            {
                CpuUsagePercent = 0f,
                MemoryUsageMB = 0f,
                ActivePlayerCount = 0,
            },
        };

        // Act
        await ((IEventHandler<ServerMetricsEvent>)_handler).Handle(@event);

        // Assert
        var saved = await _mockDataContext.ServerMetrics.SingleAsync(m => m.ServerId == serverId);
        saved.CpuUsagePercent.Should().Be(0f);
        saved.MemoryUsageMB.Should().Be(0f);
        saved.ActivePlayerCount.Should().Be(0);
    }
}
