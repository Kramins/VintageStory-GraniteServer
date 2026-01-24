using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Controllers;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Controllers;

public class ServerCollectiblesControllerTests
{
    private readonly GraniteDataContext _dataContext;
    private readonly ServerCollectiblesController _controller;
    private readonly Guid _serverId;

    public ServerCollectiblesControllerTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"CollectiblesControllerTests_{Guid.NewGuid()}")
            .Options;

        _dataContext = new GraniteDataContext(options);
        var logger = Substitute.For<ILogger<ServerCollectiblesController>>();
        _controller = new ServerCollectiblesController(logger, _dataContext);
        _serverId = Guid.NewGuid();
    }

    [Fact]
    public async Task GetAllCollectibles_ReturnsAllCollectibles()
    {
        // Arrange
        var collectibles = new[]
        {
            new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = _serverId,
                CollectibleId = 1,
                Name = "Iron Pickaxe",
                Type = "item",
                MaxStackSize = 1,
                Class = "ItemTool",
                LastSynced = DateTime.UtcNow,
            },
            new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = _serverId,
                CollectibleId = 2,
                Name = "Stone",
                Type = "block",
                MaxStackSize = 64,
                Class = "Block",
                LastSynced = DateTime.UtcNow,
            },
        };

        _dataContext.Collectibles.AddRange(collectibles);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllCollectibles(_serverId);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().NotBeNull();
        result.Value.Data.Should().HaveCount(2);
        result.Value.Data!.Should().Contain(c => c.Name == "Iron Pickaxe");
        result.Value.Data.Should().Contain(c => c.Name == "Stone");
    }

    [Fact]
    public async Task GetAllCollectibles_FilterByType_ReturnsFilteredResults()
    {
        // Arrange
        var collectibles = new[]
        {
            new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = _serverId,
                CollectibleId = 1,
                Name = "Iron Pickaxe",
                Type = "item",
                MaxStackSize = 1,
                Class = "ItemTool",
                LastSynced = DateTime.UtcNow,
            },
            new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = _serverId,
                CollectibleId = 2,
                Name = "Stone",
                Type = "block",
                MaxStackSize = 64,
                Class = "Block",
                LastSynced = DateTime.UtcNow,
            },
        };

        _dataContext.Collectibles.AddRange(collectibles);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllCollectibles(_serverId, "item");

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().NotBeNull();
        result.Value.Data.Should().HaveCount(1);
        result.Value.Data!.First().Name.Should().Be("Iron Pickaxe");
        result.Value.Data.First().Type.Should().Be("item");
    }

    [Fact]
    public async Task GetAllCollectibles_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAllCollectibles(_serverId);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().NotBeNull();
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCollectibleById_ExistingCollectible_ReturnsCollectible()
    {
        // Arrange
        var collectible = new CollectibleEntity
        {
            Id = Guid.NewGuid(),
            ServerId = _serverId,
            CollectibleId = 42,
            Name = "Diamond Sword",
            Type = "item",
            MaxStackSize = 1,
            Class = "ItemSword",
            LastSynced = DateTime.UtcNow,
        };

        _dataContext.Collectibles.Add(collectible);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCollectibleById(_serverId, 42);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().NotBeNull();
        result.Value.Data!.Id.Should().Be(42);
        result.Value.Data.Name.Should().Be("Diamond Sword");
        result.Value.Data.Type.Should().Be("item");
    }

    [Fact]
    public async Task GetCollectibleById_NonExistingCollectible_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetCollectibleById(_serverId, 999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();

        var value = notFoundResult!.Value as JsonApiDocument<string>;
        value.Should().NotBeNull();
        value!.Errors.Should().NotBeNull();
        value.Errors.Should().HaveCount(1);
        value.Errors![0].Code.Should().Be("404");
        value.Errors[0].Message.Should().Contain("999");
    }

    [Fact]
    public async Task GetAllCollectibles_SortedByName_ReturnsOrderedResults()
    {
        // Arrange
        var collectibles = new[]
        {
            new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = _serverId,
                CollectibleId = 1,
                Name = "Zinc Ingot",
                Type = "item",
                MaxStackSize = 64,
                Class = "Item",
                LastSynced = DateTime.UtcNow,
            },
            new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = _serverId,
                CollectibleId = 2,
                Name = "Apple",
                Type = "item",
                MaxStackSize = 16,
                Class = "Item",
                LastSynced = DateTime.UtcNow,
            },
        };

        _dataContext.Collectibles.AddRange(collectibles);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllCollectibles(_serverId);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().NotBeNull();
        result.Value.Data!.First().Name.Should().Be("Apple");
        result.Value.Data.Last().Name.Should().Be("Zinc Ingot");
    }
}
