using System;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Events;
using Microsoft.EntityFrameworkCore;

namespace GraniteServer.Messaging.Handlers.Events;

public class PlayerInventorySnapshotEventHandler : IEventHandler<PlayerInventorySnapshotEvent>
{
    private GraniteDataContext _dataContext;

    public PlayerInventorySnapshotEventHandler(GraniteDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task Handle(PlayerInventorySnapshotEvent command)
    {
        var eventData = command.Data!;
        var playerEntity = GetPlayerEntity(command.OriginServerId, eventData.PlayerUID);

        if (playerEntity == null)
        {
            return Task.CompletedTask;
        }

        // Clear existing inventory for this player
        var existingSlots = _dataContext
            .PlayerInventorySlots.Where(s =>
                s.PlayerId == playerEntity.Id && s.ServerId == command.OriginServerId
            )
            .ToList();
        _dataContext.PlayerInventorySlots.RemoveRange(existingSlots);

        // Insert all slots from the snapshot
        foreach (var inventory in eventData.Inventories)
        {
            foreach (var slot in inventory.Value)
            {
                var slotEntity = new PlayerInventorySlotEntity
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerEntity.Id,
                    ServerId = command.OriginServerId,
                    InventoryName = inventory.Key,
                    SlotIndex = slot.SlotIndex,
                    EntityId = slot.EntityId,
                    EntityClass = slot.EntityClass,
                    Name = slot.Name,
                    StackSize = slot.StackSize,
                    LastUpdated = DateTime.UtcNow,
                };
                _dataContext.PlayerInventorySlots.Add(slotEntity);
            }
        }

        _dataContext.SaveChanges();
        return Task.CompletedTask;
    }

    private PlayerEntity? GetPlayerEntity(Guid serverId, string playerUID)
    {
        return _dataContext.Players.FirstOrDefault(p =>
            p.PlayerUID == playerUID && p.ServerId == serverId
        );
    }
}

public class PlayerInventorySlotUpdatedEventHandler : IEventHandler<PlayerInventorySlotUpdatedEvent>
{
    private GraniteDataContext _dataContext;

    public PlayerInventorySlotUpdatedEventHandler(GraniteDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task Handle(PlayerInventorySlotUpdatedEvent command)
    {
        var eventData = command.Data!;
        var playerEntity = GetPlayerEntity(command.OriginServerId, eventData.PlayerUID);

        if (playerEntity == null)
        {
            return Task.CompletedTask;
        }

        // Upsert the slot (idempotent)
        var existingSlot = _dataContext.PlayerInventorySlots.FirstOrDefault(s =>
            s.PlayerId == playerEntity.Id
            && s.ServerId == command.OriginServerId
            && s.InventoryName == eventData.InventoryName
            && s.SlotIndex == eventData.SlotIndex
        );

        if (existingSlot != null)
        {
            existingSlot.EntityId = eventData.EntityId;
            existingSlot.EntityClass = eventData.EntityClass;
            existingSlot.Name = eventData.Name;
            existingSlot.StackSize = eventData.StackSize;
            existingSlot.LastUpdated = DateTime.UtcNow;
            _dataContext.PlayerInventorySlots.Update(existingSlot);
        }
        else
        {
            var newSlot = new PlayerInventorySlotEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = playerEntity.Id,
                ServerId = command.OriginServerId,
                InventoryName = eventData.InventoryName,
                SlotIndex = eventData.SlotIndex,
                EntityId = eventData.EntityId,
                EntityClass = eventData.EntityClass,
                Name = eventData.Name,
                StackSize = eventData.StackSize,
                LastUpdated = DateTime.UtcNow,
            };
            _dataContext.PlayerInventorySlots.Add(newSlot);
        }

        _dataContext.SaveChanges();
        return Task.CompletedTask;
    }

    private PlayerEntity? GetPlayerEntity(Guid serverId, string playerUID)
    {
        return _dataContext.Players.FirstOrDefault(p =>
            p.PlayerUID == playerUID && p.ServerId == serverId
        );
    }
}

public class PlayerInventorySlotRemovedEventHandler : IEventHandler<PlayerInventorySlotRemovedEvent>
{
    private GraniteDataContext _dataContext;

    public PlayerInventorySlotRemovedEventHandler(GraniteDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task Handle(PlayerInventorySlotRemovedEvent command)
    {
        var eventData = command.Data!;
        var playerEntity = GetPlayerEntity(command.OriginServerId, eventData.PlayerUID);

        if (playerEntity == null)
        {
            return Task.CompletedTask;
        }

        // Remove the slot from the database (idempotent)
        var existingSlot = _dataContext.PlayerInventorySlots.FirstOrDefault(s =>
            s.PlayerId == playerEntity.Id
            && s.ServerId == command.OriginServerId
            && s.InventoryName == eventData.InventoryName
            && s.SlotIndex == eventData.SlotIndex
        );

        if (existingSlot != null)
        {
            _dataContext.PlayerInventorySlots.Remove(existingSlot);
            _dataContext.SaveChanges();
        }

        return Task.CompletedTask;
    }

    private PlayerEntity? GetPlayerEntity(Guid serverId, string playerUID)
    {
        return _dataContext.Players.FirstOrDefault(p =>
            p.PlayerUID == playerUID && p.ServerId == serverId
        );
    }
}

public class CollectiblesLoadedEventHandler : IEventHandler<CollectiblesLoadedEvent>
{
    private GraniteDataContext _dataContext;

    public CollectiblesLoadedEventHandler(GraniteDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task Handle(CollectiblesLoadedEvent command)
    {
        var eventData = command.Data!;

        // Clear existing collectibles for this server
        var existingCollectibles = _dataContext
            .Collectibles.Where(c => c.ServerId == command.OriginServerId)
            .ToList();
        _dataContext.Collectibles.RemoveRange(existingCollectibles);

        var LastSynced = DateTime.UtcNow;
        // Insert all collectibles from the event
        foreach (var collectible in eventData.Collectibles)
        {
            var collectibleEntity = new CollectibleEntity
            {
                Id = Guid.NewGuid(),
                ServerId = command.OriginServerId,
                CollectibleId = collectible.Id,
                Name = collectible.Name,
                Type = collectible.Type,
                Domain = collectible.Domain,
                Path = collectible.Path,
                BlockMaterial = collectible.BlockMaterial,
                MaxStackSize = collectible.MaxStackSize,
                Class = collectible.Class,
                MapColorCode = collectible.MapColorCode,
                LastSynced = LastSynced,
            };
            _dataContext.Collectibles.Add(collectibleEntity);
        }

        _dataContext.SaveChanges();
        return Task.CompletedTask;
    }
}
