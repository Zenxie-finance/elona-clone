using ElonaClone.Game;
using ElonaClone.Game.Content;
using ElonaClone.Game.Application;

namespace ElonaClone.Game.Simulation;

public sealed class ActorProgressionState
{
    public ActorProgressionState(int level = 1, int currentExp = 0)
    {
        Level = Math.Max(1, level);
        CurrentExp = Math.Max(0, currentExp);
    }

    public int Level { get; set; }

    public int CurrentExp { get; set; }

    public int ExpToNextLevel => ProgressionCurves.GetExpToNextLevel(Level);
}

public sealed class ActorState
{
    public ActorState(
        EntityId id,
        string definitionId,
        string displayName,
        string glyph,
        string zoneId,
        GridPoint position,
        int hitPoints,
        int maxHitPoints,
        int actionPointsPerTurn,
        Faction faction,
        int attackPower,
        int defense,
        bool isPlayer = false,
        ActorProgressionState? progression = null)
    {
        Id = id;
        DefinitionId = definitionId;
        DisplayName = displayName;
        Glyph = glyph;
        ZoneId = zoneId;
        Position = position;
        HitPoints = hitPoints;
        MaxHitPoints = maxHitPoints;
        ActionPointsPerTurn = actionPointsPerTurn;
        Faction = faction;
        AttackPower = attackPower;
        Defense = defense;
        IsPlayer = isPlayer;
        IsAlive = true;
        InventoryItemIds = new List<EntityId>();
        Progression = progression ?? new ActorProgressionState();
    }

    public EntityId Id { get; }

    public string DefinitionId { get; }

    public string DisplayName { get; }

    public string Glyph { get; }

    public string ZoneId { get; set; }

    public GridPoint Position { get; set; }

    public int HitPoints { get; set; }

    public int MaxHitPoints { get; }

    public int ActionPointsPerTurn { get; }

    public Faction Faction { get; }

    public int AttackPower { get; }

    public int Defense { get; }

    public bool IsPlayer { get; }

    public bool IsAlive { get; set; }

    public int Gold { get; set; }

    public ActorProgressionState Progression { get; }

    public int Level => Progression.Level;

    public int CurrentExp => Progression.CurrentExp;

    public int ExpToNextLevel => Progression.ExpToNextLevel;

    public List<EntityId> InventoryItemIds { get; }

    public bool IsHostileTo(ActorState other) => Faction.IsHostileTo(other.Faction);
}

public sealed class ItemState
{
    public ItemState(
        EntityId id,
        string definitionId,
        string displayName,
        string glyph,
        string? zoneId,
        GridPoint? position,
        EntityId? holderId = null)
    {
        Id = id;
        DefinitionId = definitionId;
        DisplayName = displayName;
        Glyph = glyph;
        ZoneId = zoneId;
        Position = position;
        HolderId = holderId;
    }

    public EntityId Id { get; }

    public string DefinitionId { get; }

    public string DisplayName { get; }

    public string Glyph { get; }

    public string? ZoneId { get; set; }

    public GridPoint? Position { get; set; }

    public EntityId? HolderId { get; set; }
}

public sealed class QuestState
{
    public QuestState(string id, string title, string description, string targetZoneId, string turnInZoneId, int rewardGold, int rewardExperience = 0)
    {
        Id = id;
        Title = title;
        Description = description;
        TargetZoneId = targetZoneId;
        TurnInZoneId = turnInZoneId;
        RewardGold = rewardGold;
        RewardExperience = rewardExperience;
        Status = QuestStatus.Accepted;
        ObjectiveProgressValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public string TargetZoneId { get; }

    public string TurnInZoneId { get; }

    public int RewardGold { get; }

    public int RewardExperience { get; }

    public QuestStatus Status { get; set; }

    public Dictionary<string, int> ObjectiveProgressValues { get; }

    public bool IsAccepted => Status is QuestStatus.Accepted or QuestStatus.ReadyToTurnIn;

    public bool IsReadyToTurnIn => Status == QuestStatus.ReadyToTurnIn;

    public bool IsCompleted => Status == QuestStatus.Completed;

    public bool IsFailed => Status == QuestStatus.Failed;
}

public sealed class ZoneState
{
    private readonly TileType[] _tiles;

    public ZoneState(
        string id,
        string displayName,
        ZoneKind kind,
        int width,
        int height,
        GridPoint entryPoint,
        IReadOnlyList<string> connectedZoneIds,
        TileType[] tiles)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Width = width;
        Height = height;
        EntryPoint = entryPoint;
        ConnectedZoneIds = connectedZoneIds.ToArray();
        _tiles = tiles.ToArray();
    }

    public string Id { get; }

    public string DisplayName { get; }

    public ZoneKind Kind { get; }

    public int Width { get; }

    public int Height { get; }

    public GridPoint EntryPoint { get; }

    public IReadOnlyList<string> ConnectedZoneIds { get; }

    public bool IsInside(GridPoint point) => point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height;

    public bool IsWalkable(GridPoint point) => IsInside(point) && GetTile(point) != TileType.Wall;

    public TileType GetTile(GridPoint point) => _tiles[IndexOf(point)];

    public TileType[] SnapshotTiles() => _tiles.ToArray();

    private int IndexOf(GridPoint point) => point.Y * Width + point.X;
}

public sealed class WorldState
{
    public WorldState(
        string currentZoneId,
        IDictionary<string, ZoneState> zones,
        IDictionary<EntityId, ActorState> actors,
        IDictionary<EntityId, ItemState> items,
        IDictionary<string, QuestState> quests,
        int day,
        int minuteOfDay,
        string? lastVisitedZoneId = null)
    {
        CurrentZoneId = currentZoneId;
        Zones = new Dictionary<string, ZoneState>(zones, StringComparer.OrdinalIgnoreCase);
        Actors = new Dictionary<EntityId, ActorState>(actors);
        Items = new Dictionary<EntityId, ItemState>(items);
        Quests = new Dictionary<string, QuestState>(quests, StringComparer.OrdinalIgnoreCase);
        Day = day;
        MinuteOfDay = minuteOfDay;
        LastVisitedZoneId = lastVisitedZoneId;
    }

    public string CurrentZoneId { get; set; }

    public string? LastVisitedZoneId { get; set; }

    public int Day { get; set; }

    public int MinuteOfDay { get; set; }

    public Dictionary<string, ZoneState> Zones { get; }

    public Dictionary<EntityId, ActorState> Actors { get; }

    public Dictionary<EntityId, ItemState> Items { get; }

    public Dictionary<string, QuestState> Quests { get; }

    public ZoneState GetCurrentZone() => GetZone(CurrentZoneId);

    public ZoneState GetZone(string zoneId)
    {
        if (!Zones.TryGetValue(zoneId, out var zone))
        {
            throw new InvalidOperationException($"Unknown zone id '{zoneId}'.");
        }

        return zone;
    }

    public ActorState GetPlayer(EntityId playerId) => Actors[playerId];

    public IEnumerable<ActorState> GetActorsInZone(string zoneId) => Actors.Values.Where(actor => string.Equals(actor.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase) && actor.IsAlive);

    public IEnumerable<ItemState> GetItemsInZone(string zoneId) => Items.Values.Where(item => item.HolderId is null && string.Equals(item.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase) && item.Position is not null);

    public bool IsOccupied(string zoneId, GridPoint point, EntityId? ignoredActorId = null)
    {
        return GetActorsInZone(zoneId)
            .Any(actor => actor.Id != ignoredActorId && actor.Position == point);
    }

    public ActorState? FindAliveActorAt(string zoneId, GridPoint point, EntityId? ignoredActorId = null)
    {
        return GetActorsInZone(zoneId)
            .FirstOrDefault(actor => actor.Id != ignoredActorId && actor.Position == point);
    }

    public bool TryFindClosestOpenTile(string zoneId, GridPoint preferredPoint, out GridPoint openPoint)
    {
        var zone = GetZone(zoneId);
        GridPoint? bestPoint = null;
        var bestDistance = int.MaxValue;

        for (var y = 0; y < zone.Height; y++)
        {
            for (var x = 0; x < zone.Width; x++)
            {
                var candidate = new GridPoint(x, y);
                if (!zone.IsWalkable(candidate) || IsOccupied(zoneId, candidate))
                {
                    continue;
                }

                var distance = Math.Abs(candidate.X - preferredPoint.X) + Math.Abs(candidate.Y - preferredPoint.Y);
                if (bestPoint is null
                    || distance < bestDistance
                    || (distance == bestDistance && (candidate.Y < bestPoint.Value.Y
                        || (candidate.Y == bestPoint.Value.Y && candidate.X < bestPoint.Value.X))))
                {
                    bestPoint = candidate;
                    bestDistance = distance;
                }
            }
        }

        if (bestPoint is null)
        {
            openPoint = preferredPoint;
            return false;
        }

        openPoint = bestPoint.Value;
        return true;
    }

    public string? GetSuggestedTravelZoneId()
    {
        var connected = GetCurrentZone().ConnectedZoneIds;
        if (connected.Count == 0)
        {
            return null;
        }

        if (connected.Count == 1)
        {
            return connected[0];
        }

        return connected.FirstOrDefault(zoneId => !string.Equals(zoneId, LastVisitedZoneId, StringComparison.OrdinalIgnoreCase))
            ?? connected[0];
    }
}

public sealed class GameSession
{
    public GameSession(WorldState world, EntityId playerId, TurnScheduler turnScheduler, ContentCatalog content, IEnumerable<string>? initialLog = null)
    {
        World = world;
        PlayerId = playerId;
        TurnScheduler = turnScheduler;
        Content = content;
        MessageLog = new List<string>();

        if (initialLog is null)
        {
            return;
        }

        foreach (var message in initialLog)
        {
            AddMessage(message);
        }
    }

    public WorldState World { get; }

    public EntityId PlayerId { get; }

    public TurnScheduler TurnScheduler { get; }

    public ContentCatalog Content { get; }

    public List<string> MessageLog { get; }

    public void AddMessage(string message)
    {
        MessageLog.Add(message);
        if (MessageLog.Count > 24)
        {
            MessageLog.RemoveRange(0, MessageLog.Count - 24);
        }
    }

    public IReadOnlyList<string> GetRecentMessages(int count = 8) => MessageLog.TakeLast(Math.Min(count, MessageLog.Count)).ToArray();
}
