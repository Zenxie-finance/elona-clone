using ElonaClone.Game;

namespace ElonaClone.Game.Content;

public sealed record ActorDefinition(
    string Id,
    string DisplayName,
    string Glyph,
    int MaxHitPoints,
    int ActionPointsPerTurn,
    Faction Faction = Faction.Neutral,
    int AttackPower = 1,
    int Defense = 0,
    string DropItemDefinitionId = "",
    bool IsPlayerTemplate = false,
    int KillExperienceReward = 0);

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string Glyph);

public sealed record QuestDefinition(
    string Id,
    string Title,
    string Description,
    QuestObjectiveDefinition[] Objectives,
    QuestTurnInDefinition TurnIn,
    QuestRewardDefinition[] Rewards,
    QuestFailureDefinition[] Failures)
{
    public string PrimaryTargetZoneId => Objectives.FirstOrDefault()?.TargetZoneId ?? string.Empty;

    public int PrimaryGoldReward => Rewards
        .Where(reward => reward.Kind == QuestRewardKind.Gold)
        .Sum(reward => reward.Amount);

    public int PrimaryExperienceReward => Rewards
        .Where(reward => reward.Kind == QuestRewardKind.Experience)
        .Sum(reward => reward.Amount);
}

public enum QuestObjectiveKind
{
    ClearHostilesInZone,
    DefeatActorByDefinition,
    CollectItemByDefinition,
    DeliverItemByDefinition,
    ReachZone,
    EscortActorToZone,
    TalkToActor
}

public sealed record QuestObjectiveDefinition(
    string Id,
    QuestObjectiveKind Kind,
    string TargetZoneId = "",
    string TargetDefinitionId = "",
    int RequiredAmount = 1);

public enum QuestTurnInKind
{
    ManualAtZone,
    ManualAtActor,
    Automatic
}

public sealed record QuestTurnInDefinition(
    QuestTurnInKind Kind,
    string TargetZoneId = "",
    string TargetActorDefinitionId = "");

public enum QuestRewardKind
{
    Gold,
    Experience,
    Item,
    Fame,
    Karma
}

public sealed record QuestRewardDefinition(
    QuestRewardKind Kind,
    int Amount = 0,
    string TargetDefinitionId = "");

public enum QuestFailureKind
{
    None,
    TimeLimit,
    TargetDeath,
    ZoneLeave
}

public sealed record QuestFailureDefinition(
    QuestFailureKind Kind,
    int LimitMinutes = 0,
    string TargetDefinitionId = "");

public record ZoneDefinition(
    string Id,
    string DisplayName,
    ZoneKind Kind,
    string[] LayoutRows,
    GridPoint EntryPoint,
    string[] ConnectedZoneIds,
    string SpawnTableId = "");

public sealed record TownDefinition(
    string Id,
    string DisplayName,
    string[] LayoutRows,
    GridPoint EntryPoint,
    string[] ConnectedZoneIds)
    : ZoneDefinition(Id, DisplayName, ZoneKind.Town, LayoutRows, EntryPoint, ConnectedZoneIds);

public sealed record DungeonDefinition(
    string Id,
    string DisplayName,
    string[] LayoutRows,
    GridPoint EntryPoint,
    string[] ConnectedZoneIds,
    string SpawnTableId = "")
    : ZoneDefinition(Id, DisplayName, ZoneKind.Dungeon, LayoutRows, EntryPoint, ConnectedZoneIds, SpawnTableId);

public sealed record SpawnTableDefinition(
    string Id,
    string[] ActorDefinitionIds);

public static class ZoneDefinitionValidator
{
    public static void Validate(ZoneDefinition definition)
    {
        if (definition.LayoutRows.Length == 0)
        {
            throw new InvalidOperationException($"Zone '{definition.Id}' has no layout rows.");
        }

        var width = definition.LayoutRows[0].Length;
        if (width == 0)
        {
            throw new InvalidOperationException($"Zone '{definition.Id}' has an empty first row.");
        }

        for (var rowIndex = 0; rowIndex < definition.LayoutRows.Length; rowIndex++)
        {
            if (definition.LayoutRows[rowIndex].Length != width)
            {
                throw new InvalidOperationException($"Zone '{definition.Id}' uses inconsistent row widths.");
            }
        }

        var entryPoint = definition.EntryPoint;
        if (entryPoint.X < 0 || entryPoint.Y < 0 || entryPoint.X >= width || entryPoint.Y >= definition.LayoutRows.Length)
        {
            throw new InvalidOperationException($"Zone '{definition.Id}' entry point is outside the layout bounds.");
        }

        if (definition.LayoutRows[entryPoint.Y][entryPoint.X] == '#')
        {
            throw new InvalidOperationException($"Zone '{definition.Id}' entry point cannot be placed on a wall tile.");
        }
    }
}

public sealed record ContentCatalog(
    IReadOnlyDictionary<string, ActorDefinition> Actors,
    IReadOnlyDictionary<string, ItemDefinition> Items,
    IReadOnlyDictionary<string, ZoneDefinition> Zones,
    IReadOnlyDictionary<string, QuestDefinition> Quests,
    IReadOnlyDictionary<string, SpawnTableDefinition> SpawnTables);
