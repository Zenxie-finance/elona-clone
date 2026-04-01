using System.Text.Json;
using ElonaClone.Game;
using ElonaClone.Game.Content;
using ElonaClone.Game.Simulation;

namespace ElonaClone.Game.Persistence;

public sealed class SaveGameSnapshot
{
    public int Version { get; set; } = SaveLoadService.CurrentSnapshotVersion;

    public Guid PlayerId { get; set; }

    public List<Guid> TurnOrder { get; set; } = [];

    public List<string> MessageLog { get; set; } = [];

    public WorldSnapshot World { get; set; } = new();
}

public sealed class WorldSnapshot
{
    public string CurrentZoneId { get; set; } = string.Empty;

    public string? LastVisitedZoneId { get; set; }

    public int Day { get; set; }

    public int MinuteOfDay { get; set; }

    public List<ZoneSnapshot> Zones { get; set; } = [];

    public List<ActorSnapshot> Actors { get; set; } = [];

    public List<ItemSnapshot> Items { get; set; } = [];

    public List<QuestSnapshot> Quests { get; set; } = [];
}

public sealed class ZoneSnapshot
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ZoneKind Kind { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int EntryX { get; set; }

    public int EntryY { get; set; }

    public List<string> ConnectedZoneIds { get; set; } = [];

    public List<int> Tiles { get; set; } = [];
}

public sealed class ActorSnapshot
{
    public Guid Id { get; set; }

    public string DefinitionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Glyph { get; set; } = "@";

    public string ZoneId { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public int HitPoints { get; set; }

    public int MaxHitPoints { get; set; }

    public int ActionPointsPerTurn { get; set; }

    public Faction Faction { get; set; }

    public int AttackPower { get; set; }

    public int Defense { get; set; }

    public bool IsPlayer { get; set; }

    public bool IsAlive { get; set; }

    public int Gold { get; set; }

    public int Level { get; set; } = 1;

    public int CurrentExp { get; set; }

    public List<Guid> InventoryItemIds { get; set; } = [];
}

public sealed class ItemSnapshot
{
    public Guid Id { get; set; }

    public string DefinitionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Glyph { get; set; } = "!";

    public string? ZoneId { get; set; }

    public Guid? HolderId { get; set; }

    public int? X { get; set; }

    public int? Y { get; set; }
}

public sealed class QuestSnapshot
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TargetZoneId { get; set; } = string.Empty;

    public string TurnInZoneId { get; set; } = string.Empty;

    public int RewardGold { get; set; }

    public int RewardExperience { get; set; }

    public QuestStatus Status { get; set; }

    public List<QuestObjectiveProgressSnapshot> ObjectiveProgress { get; set; } = [];
}

public sealed class QuestObjectiveProgressSnapshot
{
    public string ObjectiveId { get; set; } = string.Empty;

    public int ProgressValue { get; set; }
}

public sealed class SaveLoadService
{
    public const int CurrentSnapshotVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SaveGameSnapshot CreateSnapshot(GameSession session)
    {
        return new SaveGameSnapshot
        {
            Version = CurrentSnapshotVersion,
            PlayerId = session.PlayerId.Value,
            TurnOrder = session.TurnScheduler.Snapshot().Select(actorId => actorId.Value).ToList(),
            MessageLog = session.MessageLog.ToList(),
            World = new WorldSnapshot
            {
                CurrentZoneId = session.World.CurrentZoneId,
                LastVisitedZoneId = session.World.LastVisitedZoneId,
                Day = session.World.Day,
                MinuteOfDay = session.World.MinuteOfDay,
                Zones = session.World.Zones.Values.Select(zone => new ZoneSnapshot
                {
                    Id = zone.Id,
                    DisplayName = zone.DisplayName,
                    Kind = zone.Kind,
                    Width = zone.Width,
                    Height = zone.Height,
                    EntryX = zone.EntryPoint.X,
                    EntryY = zone.EntryPoint.Y,
                    ConnectedZoneIds = zone.ConnectedZoneIds.ToList(),
                    Tiles = zone.SnapshotTiles().Select(tile => (int)tile).ToList()
                }).ToList(),
                Actors = session.World.Actors.Values.Select(actor => new ActorSnapshot
                {
                    Id = actor.Id.Value,
                    DefinitionId = actor.DefinitionId,
                    DisplayName = actor.DisplayName,
                    Glyph = actor.Glyph,
                    ZoneId = actor.ZoneId,
                    X = actor.Position.X,
                    Y = actor.Position.Y,
                    HitPoints = actor.HitPoints,
                    MaxHitPoints = actor.MaxHitPoints,
                    ActionPointsPerTurn = actor.ActionPointsPerTurn,
                    Faction = actor.Faction,
                    AttackPower = actor.AttackPower,
                    Defense = actor.Defense,
                    IsPlayer = actor.IsPlayer,
                    IsAlive = actor.IsAlive,
                    Gold = actor.Gold,
                    Level = actor.Level,
                    CurrentExp = actor.CurrentExp,
                    InventoryItemIds = actor.InventoryItemIds.Select(itemId => itemId.Value).ToList()
                }).ToList(),
                Items = session.World.Items.Values.Select(item => new ItemSnapshot
                {
                    Id = item.Id.Value,
                    DefinitionId = item.DefinitionId,
                    DisplayName = item.DisplayName,
                    Glyph = item.Glyph,
                    ZoneId = item.ZoneId,
                    HolderId = item.HolderId?.Value,
                    X = item.Position?.X,
                    Y = item.Position?.Y
                }).ToList(),
                Quests = session.World.Quests.Values.Select(quest => new QuestSnapshot
                {
                    Id = quest.Id,
                    Title = quest.Title,
                    Description = quest.Description,
                    TargetZoneId = quest.TargetZoneId,
                    TurnInZoneId = quest.TurnInZoneId,
                    RewardGold = quest.RewardGold,
                    RewardExperience = quest.RewardExperience,
                    Status = quest.Status,
                    ObjectiveProgress = quest.ObjectiveProgressValues.Select(progress => new QuestObjectiveProgressSnapshot
                    {
                        ObjectiveId = progress.Key,
                        ProgressValue = progress.Value
                    }).ToList()
                }).ToList()
            }
        };
    }

    public GameSession RestoreSnapshot(SaveGameSnapshot snapshot, ContentCatalog content)
    {
        return snapshot.Version switch
        {
            1 => RestoreSnapshot(snapshot, content, useLegacyProgressionDefaults: true),
            CurrentSnapshotVersion => RestoreSnapshot(snapshot, content, useLegacyProgressionDefaults: false),
            _ => throw new InvalidOperationException($"Unsupported save snapshot version '{snapshot.Version}'.")
        };
    }

    private GameSession RestoreSnapshot(SaveGameSnapshot snapshot, ContentCatalog content, bool useLegacyProgressionDefaults)
    {
        var zones = snapshot.World.Zones.ToDictionary(
            zone => zone.Id,
            zone => new ZoneState(
                zone.Id,
                zone.DisplayName,
                zone.Kind,
                zone.Width,
                zone.Height,
                new GridPoint(zone.EntryX, zone.EntryY),
                zone.ConnectedZoneIds,
                zone.Tiles.Select(tile => (TileType)tile).ToArray()),
            StringComparer.OrdinalIgnoreCase);

        var actors = snapshot.World.Actors.ToDictionary(
            actor => new EntityId(actor.Id),
            actor =>
            {
                var state = new ActorState(
                    new EntityId(actor.Id),
                    actor.DefinitionId,
                    actor.DisplayName,
                    actor.Glyph,
                    actor.ZoneId,
                    new GridPoint(actor.X, actor.Y),
                    actor.HitPoints,
                    actor.MaxHitPoints,
                    actor.ActionPointsPerTurn,
                    actor.Faction,
                    actor.AttackPower,
                    actor.Defense,
                    actor.IsPlayer,
                    useLegacyProgressionDefaults
                        ? new ActorProgressionState()
                        : new ActorProgressionState(actor.Level, actor.CurrentExp));

                state.IsAlive = actor.IsAlive;
                state.Gold = actor.Gold;
                state.InventoryItemIds.AddRange(actor.InventoryItemIds.Select(itemId => new EntityId(itemId)));
                return state;
            });

        var items = snapshot.World.Items.ToDictionary(
            item => new EntityId(item.Id),
            item => new ItemState(
                new EntityId(item.Id),
                item.DefinitionId,
                item.DisplayName,
                item.Glyph,
                item.ZoneId,
                item.X is not null && item.Y is not null ? new GridPoint(item.X.Value, item.Y.Value) : null,
                item.HolderId is null ? null : new EntityId(item.HolderId.Value)));

        var quests = snapshot.World.Quests.ToDictionary(
            quest => quest.Id,
            quest =>
            {
                var state = new QuestState(
                    quest.Id,
                    quest.Title,
                    quest.Description,
                    quest.TargetZoneId,
                    quest.TurnInZoneId,
                    quest.RewardGold,
                    useLegacyProgressionDefaults ? 0 : quest.RewardExperience)
                {
                    Status = quest.Status
                };

                foreach (var progress in quest.ObjectiveProgress)
                {
                    state.ObjectiveProgressValues[progress.ObjectiveId] = progress.ProgressValue;
                }

                return state;
            },
            StringComparer.OrdinalIgnoreCase);

        var world = new WorldState(
            snapshot.World.CurrentZoneId,
            zones,
            actors,
            items,
            quests,
            snapshot.World.Day,
            snapshot.World.MinuteOfDay,
            snapshot.World.LastVisitedZoneId);

        var scheduler = new TurnScheduler(snapshot.TurnOrder.Select(actorId => new EntityId(actorId)));
        return new GameSession(world, new EntityId(snapshot.PlayerId), scheduler, content, snapshot.MessageLog);
    }

    public string Serialize(SaveGameSnapshot snapshot) => JsonSerializer.Serialize(snapshot, JsonOptions);

    public SaveGameSnapshot Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SaveGameSnapshot>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize save game snapshot.");
    }
}
