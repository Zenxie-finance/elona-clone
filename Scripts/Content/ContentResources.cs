using ElonaClone.Game;
using ElonaClone.Game.Content;
using Godot;

namespace ElonaClone.Content.Resources;

[GlobalClass]
public partial class ActorDefinitionResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public string Glyph { get; set; } = "@";

    [Export]
    public int MaxHitPoints { get; set; } = 10;

    [Export]
    public int ActionPointsPerTurn { get; set; } = 100;

    [Export]
    public bool IsPlayerTemplate { get; set; }

    [Export]
    public Faction Faction { get; set; } = Faction.Neutral;

    [Export]
    public int AttackPower { get; set; } = 1;

    [Export]
    public int Defense { get; set; }

    [Export]
    public string DropItemDefinitionId { get; set; } = string.Empty;

    [Export]
    public int KillExperienceReward { get; set; }

    public ActorDefinition ToDefinition() => new(Id, DisplayName, Glyph, MaxHitPoints, ActionPointsPerTurn, Faction, AttackPower, Defense, DropItemDefinitionId, IsPlayerTemplate, KillExperienceReward);
}

[GlobalClass]
public partial class ItemDefinitionResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public string Glyph { get; set; } = "!";

    public ItemDefinition ToDefinition() => new(Id, DisplayName, Glyph);
}

[GlobalClass]
public partial class ZoneTemplateResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public ZoneKind Kind { get; set; } = ZoneKind.Home;

    [Export]
    public string[] LayoutRows { get; set; } = Array.Empty<string>();

    [Export]
    public Vector2I EntryPoint { get; set; } = Vector2I.Zero;

    [Export]
    public string[] ConnectedZoneIds { get; set; } = Array.Empty<string>();

    [Export]
    public string SpawnTableId { get; set; } = string.Empty;

    public virtual ZoneDefinition ToDefinition() => new(
        Id,
        DisplayName,
        Kind,
        LayoutRows,
        new GridPoint(EntryPoint.X, EntryPoint.Y),
        ConnectedZoneIds,
        SpawnTableId);
}

[GlobalClass]
public partial class TownDefinitionResource : ZoneTemplateResource
{
    public override ZoneDefinition ToDefinition() => new TownDefinition(
        Id,
        DisplayName,
        LayoutRows,
        new GridPoint(EntryPoint.X, EntryPoint.Y),
        ConnectedZoneIds);
}

[GlobalClass]
public partial class DungeonTemplateResource : ZoneTemplateResource
{
    public override ZoneDefinition ToDefinition() => new DungeonDefinition(
        Id,
        DisplayName,
        LayoutRows,
        new GridPoint(EntryPoint.X, EntryPoint.Y),
        ConnectedZoneIds,
        SpawnTableId);
}

[GlobalClass]
public partial class QuestDefinitionResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string Title { get; set; } = string.Empty;

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Export]
    public QuestObjectiveKind ObjectiveKind { get; set; } = QuestObjectiveKind.ClearHostilesInZone;

    [Export]
    public string ObjectiveTargetZoneId { get; set; } = string.Empty;

    [Export]
    public QuestTurnInKind TurnInKind { get; set; } = QuestTurnInKind.ManualAtZone;

    [Export]
    public string TurnInZoneId { get; set; } = string.Empty;

    [Export]
    public int RewardGold { get; set; } = 100;

    [Export]
    public int RewardExperience { get; set; }

    public QuestDefinition ToDefinition()
    {
        var rewards = new List<QuestRewardDefinition>();
        if (RewardGold > 0)
        {
            rewards.Add(new QuestRewardDefinition(QuestRewardKind.Gold, RewardGold));
        }

        if (RewardExperience > 0)
        {
            rewards.Add(new QuestRewardDefinition(QuestRewardKind.Experience, RewardExperience));
        }

        return new QuestDefinition(
            Id,
            Title,
            Description,
            new[]
            {
                new QuestObjectiveDefinition(
                    "primary",
                    ObjectiveKind,
                    ObjectiveTargetZoneId,
                    RequiredAmount: 1)
            },
            new QuestTurnInDefinition(TurnInKind, TurnInZoneId),
            rewards.ToArray(),
            Array.Empty<QuestFailureDefinition>());
    }
}

[GlobalClass]
public partial class SpawnTableResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string[] ActorDefinitionIds { get; set; } = Array.Empty<string>();

    public SpawnTableDefinition ToDefinition() => new(Id, ActorDefinitionIds);
}
