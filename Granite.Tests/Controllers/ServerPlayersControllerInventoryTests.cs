using System;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Controllers;
using Granite.Server.Services;
using GraniteServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Sieve.Models;
using Sieve.Services;
using Xunit;

namespace Granite.Tests.Controllers;

public class ServerPlayersControllerInventoryTests
{
    private readonly ServerPlayersController _controller;
    private readonly ServerPlayersService _mockService;
    private readonly Guid _serverId;

    public ServerPlayersControllerInventoryTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"PlayerControllerInventoryTests_{Guid.NewGuid()}")
            .Options;

        var dataContext = new GraniteDataContext(options);
        _mockService = Substitute.For<ServerPlayersService>(null!, null!, null!);
        
        // Create real SieveProcessor with default options
        var sieveOptions = Options.Create(new SieveOptions());
        var sieveProcessor = new SieveProcessor(sieveOptions);
        
        var logger = Substitute.For<ILogger<ServerPlayersController>>();

        _controller = new ServerPlayersController(logger, _mockService, sieveProcessor);
        _serverId = Guid.NewGuid();
    }

    [Fact]
    public async Task UpdateInventorySlot_ValidRequest_ReturnsOkWithMessage()
    {
        // Arrange
        var playerId = "player123";
        var inventoryName = "backpack";
        var slotIndex = 5;
        var request = new UpdateInventorySlotRequestDTO
        {
            EntityId = 42,
            EntityClass = "game:pickaxe-iron",
            StackSize = 1,
        };

        _mockService
            .UpdateInventorySlot(_serverId, playerId, inventoryName, slotIndex, request)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateInventorySlot(
            _serverId,
            playerId,
            inventoryName,
            slotIndex,
            request
        );

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().Contain("updated successfully");
        await _mockService
            .Received(1)
            .UpdateInventorySlot(_serverId, playerId, inventoryName, slotIndex, request);
    }

    [Fact]
    public async Task RemoveInventorySlot_ValidRequest_ReturnsOkWithMessage()
    {
        // Arrange
        var playerId = "player456";
        var inventoryName = "hotbar";
        var slotIndex = 2;

        _mockService
            .RemoveInventorySlot(_serverId, playerId, inventoryName, slotIndex)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveInventorySlot(
            _serverId,
            playerId,
            inventoryName,
            slotIndex
        );

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().Contain("removed successfully");
        await _mockService
            .Received(1)
            .RemoveInventorySlot(_serverId, playerId, inventoryName, slotIndex);
    }

    [Fact]
    public async Task UpdateInventorySlot_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var playerId = "testPlayer";
        var inventoryName = "character";
        var slotIndex = 10;
        var request = new UpdateInventorySlotRequestDTO
        {
            EntityId = 99,
            EntityClass = "game:sword-iron",
            StackSize = 1,
        };

        // Act
        await _controller.UpdateInventorySlot(
            _serverId,
            playerId,
            inventoryName,
            slotIndex,
            request
        );

        // Assert
        await _mockService
            .Received(1)
            .UpdateInventorySlot(
                Arg.Is<Guid>(id => id == _serverId),
                Arg.Is<string>(id => id == playerId),
                Arg.Is<string>(name => name == inventoryName),
                Arg.Is<int>(idx => idx == slotIndex),
                Arg.Is<UpdateInventorySlotRequestDTO>(req =>
                    req.EntityId == 99 && req.EntityClass == "game:sword-iron" && req.StackSize == 1
                )
            );
    }

    [Fact]
    public async Task RemoveInventorySlot_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var playerId = "anotherPlayer";
        var inventoryName = "basket";
        var slotIndex = 7;

        // Act
        await _controller.RemoveInventorySlot(_serverId, playerId, inventoryName, slotIndex);

        // Assert
        await _mockService
            .Received(1)
            .RemoveInventorySlot(
                Arg.Is<Guid>(id => id == _serverId),
                Arg.Is<string>(id => id == playerId),
                Arg.Is<string>(name => name == inventoryName),
                Arg.Is<int>(idx => idx == slotIndex)
            );
    }
}
