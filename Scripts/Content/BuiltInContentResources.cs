using ElonaClone.Content.Resources;
using ElonaClone.Game;
using ElonaClone.Game.Content;

namespace ElonaClone.Content;

public sealed class BuiltInContentBundle
{
    public BuiltInContentBundle(
        ActorDefinitionResource[] actors,
        ItemDefinitionResource[] items,
        ZoneTemplateResource[] zones,
        QuestDefinitionResource[] quests,
        SpawnTableResource[] spawnTables)
    {
        Actors = actors;
        Items = items;
        Zones = zones;
        Quests = quests;
        SpawnTables = spawnTables;
    }

    public ActorDefinitionResource[] Actors { get; }

    public ItemDefinitionResource[] Items { get; }

    public ZoneTemplateResource[] Zones { get; }

    public QuestDefinitionResource[] Quests { get; }

    public SpawnTableResource[] SpawnTables { get; }
}

public static class BuiltInContentResources
{
    public static BuiltInContentBundle Create()
    {
        return new BuiltInContentBundle(
            new[]
            {
                new ActorDefinitionResource
                {
                    Id = "player",
                    DisplayName = "Adventurer",
                    Glyph = "@",
                    MaxHitPoints = 20,
                    ActionPointsPerTurn = 100,
                    Faction = Faction.Player,
                    AttackPower = 4,
                    Defense = 1,
                    IsPlayerTemplate = true
                },
                new ActorDefinitionResource
                {
                    Id = "putit",
                    DisplayName = "Putit",
                    Glyph = "p",
                    MaxHitPoints = 5,
                    ActionPointsPerTurn = 80,
                    Faction = Faction.Hostile,
                    AttackPower = 2,
                    Defense = 0,
                    DropItemDefinitionId = "putit_slime",
                    KillExperienceReward = 25
                }
            },
            new[]
            {
                new ItemDefinitionResource
                {
                    Id = "ration",
                    DisplayName = "Ration",
                    Glyph = "!"
                },
                new ItemDefinitionResource
                {
                    Id = "short_sword",
                    DisplayName = "Short Sword",
                    Glyph = "/"
                },
                new ItemDefinitionResource
                {
                    Id = "putit_slime",
                    DisplayName = "Putit Slime",
                    Glyph = "%"
                }
            },
            new ZoneTemplateResource[]
            {
                new ZoneTemplateResource
                {
                    Id = "home",
                    DisplayName = "Home",
                    Kind = ZoneKind.Home,
                    LayoutRows = new[]
                    {
                        "#########",
                        "#.......#",
                        "#.......#",
                        "#.......#",
                        "#.......#",
                        "#.......#",
                        "#########"
                    },
                    EntryPoint = new Godot.Vector2I(4, 3),
                    ConnectedZoneIds = new[] { "vernis" }
                },
                new TownDefinitionResource
                {
                    Id = "vernis",
                    DisplayName = "Vernis",
                    LayoutRows = new[]
                    {
                        "###########",
                        "#.........#",
                        "#.........#",
                        "#.........#",
                        "#.........#",
                        "#.........#",
                        "###########"
                    },
                    EntryPoint = new Godot.Vector2I(5, 3),
                    ConnectedZoneIds = new[] { "home", "puppy_cave" }
                },
                new DungeonTemplateResource
                {
                    Id = "puppy_cave",
                    DisplayName = "Puppy Cave",
                    LayoutRows = new[]
                    {
                        "##########",
                        "#........#",
                        "#..##....#",
                        "#........#",
                        "#....##..#",
                        "#........#",
                        "##########"
                    },
                    EntryPoint = new Godot.Vector2I(1, 1),
                    ConnectedZoneIds = new[] { "vernis" },
                    SpawnTableId = "cave_low_level"
                }
            },
            new[]
            {
                new QuestDefinitionResource
                {
                    Id = "vermin_cleanup",
                    Title = "Vermin Cleanup",
                    Description = "Travel to the first cave, survive, and bring your loot back to town.",
                    ObjectiveKind = QuestObjectiveKind.ClearHostilesInZone,
                    ObjectiveTargetZoneId = "puppy_cave",
                    TurnInKind = QuestTurnInKind.ManualAtZone,
                    TurnInZoneId = "vernis",
                    RewardGold = 500,
                    RewardExperience = 90
                }
            },
            new[]
            {
                new SpawnTableResource
                {
                    Id = "cave_low_level",
                    ActorDefinitionIds = new[] { "putit" }
                }
            });
    }
}
