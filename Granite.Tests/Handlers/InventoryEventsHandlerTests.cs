using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granite.Tests.Handlers;

public class InventoryEventsHandlerTests
{
    private readonly GraniteDataContext _dataContext;
    private readonly PlayerInventorySnapshotEventHandler _snapshotHandler;
    private readonly PlayerInventorySlotUpdatedEventHandler _slotUpdatedHandler;
    private readonly PlayerInventorySlotRemovedEventHandler _slotRemovedHandler;
    private readonly CollectiblesLoadedEventHandler _collectiblesHandler;

    public InventoryEventsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"InventoryEventsTests_{Guid.NewGuid()}")
            .Options;

        _dataContext = new GraniteDataContext(options);
        _snapshotHandler = new PlayerInventorySnapshotEventHandler(_dataContext);
        _slotUpdatedHandler = new PlayerInventorySlotUpdatedEventHandler(_dataContext);
        _slotRemovedHandler = new PlayerInventorySlotRemovedEventHandler(_dataContext);
        _collectiblesHandler = new CollectiblesLoadedEventHandler(_dataContext);
    }

    [Fact]
    public async Task Handle_PlayerInventorySnapshotEvent_CreatesInventorySlots()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySnapshotEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySnapshotEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                Inventories = new Dictionary<string, List<InventorySlotEventData>>
                {
                    {
                        "backpack",
                        new List<InventorySlotEventData>
                        {
                            new InventorySlotEventData
                            {
                                SlotIndex = 0,
                                EntityId = 1,
                                EntityClass = "game:pickaxe-iron",
                                Name = "Iron Pickaxe",
                                StackSize = 1
                            },
                            new InventorySlotEventData
                            {
                                SlotIndex = 1,
                                EntityId = 2,
                                EntityClass = "game:stone-granite",
                                Name = "Granite",
                                StackSize = 64
                            }
                        }
                    }
                }
            }
        };

        // Act
        await _snapshotHandler.Handle(@event);

        // Assert
        var savedSlots = _dataContext.PlayerInventorySlots
            .Where(s => s.PlayerId == player.Id && s.ServerId == serverId)
            .ToList();
        savedSlots.Should().HaveCount(2);
        savedSlots.Should().Contain(s => s.InventoryName == "backpack" && s.SlotIndex == 0 && s.EntityId == 1);
        savedSlots.Should().Contain(s => s.InventoryName == "backpack" && s.SlotIndex == 1 && s.EntityId == 2);
    }

    [Fact]
    public async Task Handle_PlayerInventorySnapshotEvent_ClearsExistingInventory()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);

        // Add existing inventory slots
        var existingSlot = new PlayerInventorySlotEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ServerId = serverId,
            InventoryName = "backpack",
            SlotIndex = 0,
            EntityId = 999,
            EntityClass = "game:old-item",
            Name = "Old Item",
            StackSize = 1,
            LastUpdated = DateTime.UtcNow
        };
        _dataContext.PlayerInventorySlots.Add(existingSlot);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySnapshotEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySnapshotEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                Inventories = new Dictionary<string, List<InventorySlotEventData>>
                {
                    {
                        "backpack",
                        new List<InventorySlotEventData>
                        {
                            new InventorySlotEventData
                            {
                                SlotIndex = 0,
                                EntityId = 1,
                                EntityClass = "game:new-item",
                                Name = "New Item",
                                StackSize = 1
                            }
                        }
                    }
                }
            }
        };

        // Act
        await _snapshotHandler.Handle(@event);

        // Assert
        var savedSlots = _dataContext.PlayerInventorySlots
            .Where(s => s.PlayerId == player.Id && s.ServerId == serverId)
            .ToList();
        savedSlots.Should().HaveCount(1);
        savedSlots[0].EntityId.Should().Be(1);
        savedSlots[0].Name.Should().Be("New Item");
    }

    [Fact]
    public async Task Handle_PlayerInventorySnapshotEvent_PlayerNotFound_DoesNothing()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var @event = new PlayerInventorySnapshotEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySnapshotEventData
            {
                PlayerUID = "nonexistent",
                PlayerName = "Ghost",
                IpAddress = "192.168.1.1",
                Inventories = new Dictionary<string, List<InventorySlotEventData>>()
            }
        };

        // Act
        await _snapshotHandler.Handle(@event);

        // Assert
        _dataContext.PlayerInventorySlots.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PlayerInventorySlotUpdatedEvent_CreatesNewSlot()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySlotUpdatedEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySlotUpdatedEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                InventoryName = "backpack",
                SlotIndex = 5,
                EntityId = 42,
                EntityClass = "game:sword-iron",
                Name = "Iron Sword",
                StackSize = 1
            }
        };

        // Act
        await _slotUpdatedHandler.Handle(@event);

        // Assert
        var savedSlot = _dataContext.PlayerInventorySlots
            .Single(s => s.PlayerId == player.Id && s.InventoryName == "backpack" && s.SlotIndex == 5);
        savedSlot.EntityId.Should().Be(42);
        savedSlot.Name.Should().Be("Iron Sword");
        savedSlot.StackSize.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PlayerInventorySlotUpdatedEvent_UpdatesExistingSlot()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);

        var existingSlot = new PlayerInventorySlotEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ServerId = serverId,
            InventoryName = "backpack",
            SlotIndex = 5,
            EntityId = 10,
            EntityClass = "game:stone",
            Name = "Stone",
            StackSize = 32,
            LastUpdated = DateTime.UtcNow.AddMinutes(-10)
        };
        _dataContext.PlayerInventorySlots.Add(existingSlot);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySlotUpdatedEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySlotUpdatedEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                InventoryName = "backpack",
                SlotIndex = 5,
                EntityId = 10,
                EntityClass = "game:stone",
                Name = "Stone",
                StackSize = 64
            }
        };

        // Act
        await _slotUpdatedHandler.Handle(@event);

        // Assert
        var updatedSlot = _dataContext.PlayerInventorySlots
            .Single(s => s.PlayerId == player.Id && s.InventoryName == "backpack" && s.SlotIndex == 5);
        updatedSlot.StackSize.Should().Be(64);
        updatedSlot.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_PlayerInventorySlotRemovedEvent_RemovesSlot()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);

        var slotToRemove = new PlayerInventorySlotEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ServerId = serverId,
            InventoryName = "backpack",
            SlotIndex = 3,
            EntityId = 15,
            EntityClass = "game:axe",
            Name = "Axe",
            StackSize = 1,
            LastUpdated = DateTime.UtcNow
        };
        _dataContext.PlayerInventorySlots.Add(slotToRemove);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySlotRemovedEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySlotRemovedEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                InventoryName = "backpack",
                SlotIndex = 3
            }
        };

        // Act
        await _slotRemovedHandler.Handle(@event);

        // Assert
        _dataContext.PlayerInventorySlots
            .Should()
            .NotContain(s => s.PlayerId == player.Id && s.InventoryName == "backpack" && s.SlotIndex == 3);
    }

    [Fact]
    public async Task Handle_PlayerInventorySlotRemovedEvent_SlotNotFound_DoesNotThrow()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySlotRemovedEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySlotRemovedEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                InventoryName = "backpack",
                SlotIndex = 999
            }
        };

        // Act & Assert
        await _slotRemovedHandler.Handle(@event);
        // Should not throw
    }

    [Fact]
    public async Task Handle_CollectiblesLoadedEvent_AddsCollectibles()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var @event = new CollectiblesLoadedEvent
        {
            OriginServerId = serverId,
            Data = new CollectiblesLoadedEventData
            {
                Collectibles = new List<CollectibleEventData>
                {
                    new CollectibleEventData
                    {
                        Id = 1,
                        Name = "Iron Pickaxe",
                        Type = "item",
                        MaxStackSize = 1,
                        Class = "ItemTool"
                    },
                    new CollectibleEventData
                    {
                        Id = 2,
                        Name = "Stone",
                        Type = "block",
                        MaxStackSize = 64,
                        Class = "Block"
                    }
                }
            }
        };

        // Act
        await _collectiblesHandler.Handle(@event);

        // Assert
        var savedCollectibles = _dataContext.Collectibles
            .Where(c => c.ServerId == serverId)
            .ToList();
        savedCollectibles.Should().HaveCount(2);
        savedCollectibles.Should().Contain(c => c.CollectibleId == 1 && c.Name == "Iron Pickaxe");
        savedCollectibles.Should().Contain(c => c.CollectibleId == 2 && c.Name == "Stone");
    }

    [Fact]
    public async Task Handle_CollectiblesLoadedEvent_ReplacesExistingCollectibles()
    {
        // Arrange
        var serverId = Guid.NewGuid();

        // Add existing collectibles
        var oldCollectible = new CollectibleEntity
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            CollectibleId = 999,
            Name = "Old Item",
            Type = "item",
            MaxStackSize = 1,
            Class = "Item",
            LastSynced = DateTime.UtcNow.AddDays(-1)
        };
        _dataContext.Collectibles.Add(oldCollectible);
        _dataContext.SaveChanges();

        var @event = new CollectiblesLoadedEvent
        {
            OriginServerId = serverId,
            Data = new CollectiblesLoadedEventData
            {
                Collectibles = new List<CollectibleEventData>
                {
                    new CollectibleEventData
                    {
                        Id = 1,
                        Name = "New Item",
                        Type = "item",
                        MaxStackSize = 1,
                        Class = "Item"
                    }
                }
            }
        };

        // Act
        await _collectiblesHandler.Handle(@event);

        // Assert
        var savedCollectibles = _dataContext.Collectibles
            .Where(c => c.ServerId == serverId)
            .ToList();
        savedCollectibles.Should().HaveCount(1);
        savedCollectibles[0].CollectibleId.Should().Be(1);
        savedCollectibles[0].Name.Should().Be("New Item");
    }

    [Fact]
    public async Task Handle_CollectiblesLoadedEvent_EmptyList_ClearsAllCollectibles()
    {
        // Arrange
        var serverId = Guid.NewGuid();

        var existingCollectible = new CollectibleEntity
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            CollectibleId = 1,
            Name = "Item",
            Type = "item",
            MaxStackSize = 1,
            Class = "Item",
            LastSynced = DateTime.UtcNow
        };
        _dataContext.Collectibles.Add(existingCollectible);
        _dataContext.SaveChanges();

        var @event = new CollectiblesLoadedEvent
        {
            OriginServerId = serverId,
            Data = new CollectiblesLoadedEventData
            {
                Collectibles = new List<CollectibleEventData>()
            }
        };

        // Act
        await _collectiblesHandler.Handle(@event);

        // Assert
        _dataContext.Collectibles
            .Where(c => c.ServerId == serverId)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleInventories_StoresCorrectly()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = playerUID,
            ServerId = serverId,
            Name = "TestPlayer",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow
        };
        _dataContext.Players.Add(player);
        _dataContext.SaveChanges();

        var @event = new PlayerInventorySnapshotEvent
        {
            OriginServerId = serverId,
            Data = new PlayerInventorySnapshotEventData
            {
                PlayerUID = playerUID,
                PlayerName = "TestPlayer",
                IpAddress = "192.168.1.1",
                Inventories = new Dictionary<string, List<InventorySlotEventData>>
                {
                    {
                        "backpack",
                        new List<InventorySlotEventData>
                        {
                            new InventorySlotEventData { SlotIndex = 0, EntityId = 1, Name = "Item 1", StackSize = 1 }
                        }
                    },
                    {
                        "hotbar",
                        new List<InventorySlotEventData>
                        {
                            new InventorySlotEventData { SlotIndex = 0, EntityId = 2, Name = "Item 2", StackSize = 1 }
                        }
                    }
                }
            }
        };

        // Act
        await _snapshotHandler.Handle(@event);

        // Assert
        var backpackSlots = _dataContext.PlayerInventorySlots
            .Where(s => s.PlayerId == player.Id && s.InventoryName == "backpack")
            .ToList();
        var hotbarSlots = _dataContext.PlayerInventorySlots
            .Where(s => s.PlayerId == player.Id && s.InventoryName == "hotbar")
            .ToList();

        backpackSlots.Should().HaveCount(1);
        hotbarSlots.Should().HaveCount(1);
    }
}
