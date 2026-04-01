using ElonaClone.Game;
using ElonaClone.Game.Content;
using ElonaClone.Game.Simulation;

namespace ElonaClone.Game.Application;

public interface IRngService
{
    int NextInt(int minInclusive, int maxExclusive);
}

public sealed class DefaultRngService : IRngService
{
    private readonly Random _random;

    public DefaultRngService(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}

public interface IClockService
{
    int InitialDay { get; }

    int InitialMinuteOfDay { get; }
}

public sealed record FixedClockService(int InitialDay = 1, int InitialMinuteOfDay = 8 * 60) : IClockService;

public sealed class ZoneAssembler
{
    public ZoneState Assemble(ZoneDefinition definition)
    {
        ZoneDefinitionValidator.Validate(definition);
        var width = definition.LayoutRows[0].Length;
        var tiles = new List<TileType>(width * definition.LayoutRows.Length);
        foreach (var row in definition.LayoutRows)
        {
            foreach (var tile in row)
            {
                tiles.Add(tile == '#' ? TileType.Wall : TileType.Floor);
            }
        }

        return new ZoneState(
            definition.Id,
            definition.DisplayName,
            definition.Kind,
            width,
            definition.LayoutRows.Length,
            definition.EntryPoint,
            definition.ConnectedZoneIds,
            tiles.ToArray());
    }
}

public sealed class QuestBoardService
{
    public IReadOnlyDictionary<string, QuestState> CreateStartingQuests(ContentCatalog catalog)
    {
        return catalog.Quests.Values.ToDictionary(
            quest => quest.Id,
            quest =>
            {
                var state = new QuestState(
                    quest.Id,
                    quest.Title,
                    quest.Description,
                    quest.PrimaryTargetZoneId,
                    quest.TurnIn.TargetZoneId,
                    quest.PrimaryGoldReward,
                    quest.PrimaryExperienceReward);

                foreach (var objective in quest.Objectives)
                {
                    state.ObjectiveProgressValues[objective.Id] = 0;
                }

                return state;
            },
            StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class SessionFactory
{
    private readonly IClockService _clockService;
    private readonly IRngService _rngService;
    private readonly ZoneAssembler _zoneAssembler;
    private readonly QuestBoardService _questBoardService;

    public SessionFactory(
        IClockService clockService,
        IRngService rngService,
        ZoneAssembler zoneAssembler,
        QuestBoardService questBoardService)
    {
        _clockService = clockService;
        _rngService = rngService;
        _zoneAssembler = zoneAssembler;
        _questBoardService = questBoardService;
    }

    public GameSession CreateNewSession(ContentCatalog catalog)
    {
        var zones = catalog.Zones.Values.ToDictionary(
            zone => zone.Id,
            zone => _zoneAssembler.Assemble(zone),
            StringComparer.OrdinalIgnoreCase);

        var startingZone = zones.TryGetValue("home", out var homeZone)
            ? homeZone
            : zones.Values.First();

        var playerDefinition = catalog.Actors.Values.FirstOrDefault(actor => actor.IsPlayerTemplate)
            ?? catalog.Actors.Values.First();

        var player = new ActorState(
            EntityId.New(),
            playerDefinition.Id,
            playerDefinition.DisplayName,
            playerDefinition.Glyph,
            startingZone.Id,
            startingZone.EntryPoint,
            playerDefinition.MaxHitPoints,
            playerDefinition.MaxHitPoints,
            playerDefinition.ActionPointsPerTurn,
            playerDefinition.Faction,
            playerDefinition.AttackPower,
            playerDefinition.Defense,
            playerDefinition.IsPlayerTemplate);

        var actors = new Dictionary<EntityId, ActorState>
        {
            [player.Id] = player
        };

        SpawnZoneActors(catalog, zones, actors);

        var items = CreateStartingItems(catalog, startingZone, player);
        var quests = new Dictionary<string, QuestState>(_questBoardService.CreateStartingQuests(catalog), StringComparer.OrdinalIgnoreCase);
        var world = new WorldState(startingZone.Id, zones, actors, items, quests, _clockService.InitialDay, _clockService.InitialMinuteOfDay);
        var turnScheduler = new TurnScheduler(new[] { player.Id });

        return new GameSession(
            world,
            player.Id,
            turnScheduler,
            catalog,
            new[]
            {
                "Bootstrapped runtime skeleton.",
                "Move with arrow keys. Bump hostile targets to attack. Travel with T. Pick up with G. Drop with D. Claim ready quests with C."
            });
    }

    private void SpawnZoneActors(
        ContentCatalog catalog,
        IReadOnlyDictionary<string, ZoneState> zones,
        IDictionary<EntityId, ActorState> actors)
    {
        foreach (var zoneDefinition in catalog.Zones.Values)
        {
            if (string.IsNullOrWhiteSpace(zoneDefinition.SpawnTableId))
            {
                continue;
            }

            if (!catalog.SpawnTables.TryGetValue(zoneDefinition.SpawnTableId, out var spawnTable))
            {
                continue;
            }

            if (!zones.TryGetValue(zoneDefinition.Id, out var zone))
            {
                continue;
            }

            foreach (var actorDefinitionId in spawnTable.ActorDefinitionIds)
            {
                if (!catalog.Actors.TryGetValue(actorDefinitionId, out var actorDefinition))
                {
                    continue;
                }

                var spawnPoint = FindOpenTile(zone, actors.Values);
                var spawnedActor = new ActorState(
                    EntityId.New(),
                    actorDefinition.Id,
                    actorDefinition.DisplayName,
                    actorDefinition.Glyph,
                    zone.Id,
                    spawnPoint,
                    actorDefinition.MaxHitPoints,
                    actorDefinition.MaxHitPoints,
                    actorDefinition.ActionPointsPerTurn,
                    actorDefinition.Faction,
                    actorDefinition.AttackPower,
                    actorDefinition.Defense);

                actors[spawnedActor.Id] = spawnedActor;
            }
        }
    }

    private Dictionary<EntityId, ItemState> CreateStartingItems(ContentCatalog catalog, ZoneState startingZone, ActorState player)
    {
        var items = new Dictionary<EntityId, ItemState>();

        if (catalog.Items.TryGetValue("ration", out var rationDefinition))
        {
            var ration = new ItemState(EntityId.New(), rationDefinition.Id, rationDefinition.DisplayName, rationDefinition.Glyph, null, null, player.Id);
            player.InventoryItemIds.Add(ration.Id);
            items[ration.Id] = ration;
        }

        if (catalog.Items.TryGetValue("short_sword", out var swordDefinition))
        {
            var sword = new ItemState(EntityId.New(), swordDefinition.Id, swordDefinition.DisplayName, swordDefinition.Glyph, startingZone.Id, startingZone.EntryPoint);
            items[sword.Id] = sword;
        }

        return items;
    }

    private GridPoint FindOpenTile(ZoneState zone, IEnumerable<ActorState> existingActors)
    {
        var availableTiles = new List<GridPoint>();
        for (var y = 0; y < zone.Height; y++)
        {
            for (var x = 0; x < zone.Width; x++)
            {
                var point = new GridPoint(x, y);
                if (!zone.IsWalkable(point))
                {
                    continue;
                }

                if (existingActors.Any(actor => string.Equals(actor.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase) && actor.Position == point))
                {
                    continue;
                }

                availableTiles.Add(point);
            }
        }

        if (availableTiles.Count == 0)
        {
            return zone.EntryPoint;
        }

        var index = _rngService.NextInt(0, availableTiles.Count);
        return availableTiles[index];
    }
}

public sealed class GameBootstrap
{
    private readonly SessionFactory _sessionFactory;

    public GameBootstrap(SessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public GameSession CreateDefaultSession(ContentCatalog catalog) => _sessionFactory.CreateNewSession(catalog);
}
