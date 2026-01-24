using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Controllers;
using Granite.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Controllers;

public class ServerConfigControllerTests
{
    private readonly ServerConfigService _mockService;
    private readonly ServerConfigController _controller;

    public ServerConfigControllerTests()
    {
        _mockService = Substitute.For<ServerConfigService>(null!, null!, null!);
        var logger = Substitute.For<ILogger<ServerConfigController>>();
        _controller = new ServerConfigController(logger, _mockService);
    }

    [Fact]
    public async Task GetServerConfig_ServerExists_ReturnsOkWithConfig()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var expectedConfig = new ServerConfigDTO
        {
            ServerName = "TestServer",
            Port = 12345,
            MaxClients = 32,
        };

        _mockService.GetServerConfigAsync(serverId).Returns(expectedConfig);

        // Act
        var result = await _controller.GetServerConfig(serverId);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().NotBeNull();
        result.Value.Data!.ServerName.Should().Be("TestServer");
        result.Value.Data.Port.Should().Be(12345);
        result.Value.Data.MaxClients.Should().Be(32);
    }

    [Fact]
    public async Task GetServerConfig_ServerDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockService.GetServerConfigAsync(serverId).Returns((ServerConfigDTO?)null);

        // Act
        var result = await _controller.GetServerConfig(serverId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();

        var value = notFoundResult!.Value as JsonApiDocument<ServerConfigDTO>;
        value.Should().NotBeNull();
        value!.Errors.Should().NotBeNull();
        value.Errors.Should().HaveCount(1);
        value.Errors![0].Code.Should().Be("404");
        value.Errors[0].Message.Should().Contain(serverId.ToString());
    }

    [Fact]
    public async Task SyncServerConfig_ValidRequest_ReturnsOkWithMessage()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockService.SyncServerConfigAsync(serverId).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SyncServerConfig(serverId);

        // Assert
        result.Should().NotBeNull();

        var value = result.Value;
        value.Should().NotBeNull();
        value!.Data.Should().NotBeNull();

        await _mockService.Received(1).SyncServerConfigAsync(serverId);
    }

    [Fact]
    public async Task UpdateServerConfig_ValidConfig_ReturnsOkWithMessage()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var config = new ServerConfigDTO
        {
            ServerName = "UpdatedServer",
            MaxClients = 64,
            AllowPvP = true,
        };

        _mockService.UpdateServerConfigAsync(serverId, config).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateServerConfig(serverId, config);

        // Assert
        result.Should().NotBeNull();

        var value = result.Value;
        value.Should().NotBeNull();
        value!.Data.Should().NotBeNull();

        await _mockService.Received(1).UpdateServerConfigAsync(serverId, config);
    }

    [Fact]
    public async Task UpdateServerConfig_PartialUpdate_OnlyUpdatesSpecifiedFields()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var config = new ServerConfigDTO
        {
            ServerName = "NewName",
            MaxClients = null, // Not updated
            Port = null, // Not updated
        };

        _mockService.UpdateServerConfigAsync(serverId, config).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateServerConfig(serverId, config);

        // Assert
        await _mockService
            .Received(1)
            .UpdateServerConfigAsync(
                Arg.Is<Guid>(id => id == serverId),
                Arg.Is<ServerConfigDTO>(c =>
                    c.ServerName == "NewName" && c.MaxClients == null && c.Port == null
                )
            );
    }

    [Fact]
    public async Task UpdateServerConfig_EmptyConfig_StillCallsService()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var config = new ServerConfigDTO();

        _mockService.UpdateServerConfigAsync(serverId, config).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateServerConfig(serverId, config);

        // Assert
        await _mockService.Received(1).UpdateServerConfigAsync(serverId, config);
        var value = result.Value;
        value.Should().NotBeNull();
    }
}
