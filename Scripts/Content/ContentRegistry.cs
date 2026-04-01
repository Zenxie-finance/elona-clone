using ElonaClone.Game.Content;

namespace ElonaClone.Content;

public sealed class ContentRegistry
{
    public ContentCatalog LoadBuiltInCatalog()
    {
        var bundle = BuiltInContentResources.Create();
        var actors = bundle.Actors.Select(resource => resource.ToDefinition()).ToDictionary(actor => actor.Id, StringComparer.OrdinalIgnoreCase);
        var items = bundle.Items.Select(resource => resource.ToDefinition()).ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var zones = bundle.Zones.Select(resource => resource.ToDefinition()).ToDictionary(zone => zone.Id, StringComparer.OrdinalIgnoreCase);
        var quests = bundle.Quests.Select(resource => resource.ToDefinition()).ToDictionary(quest => quest.Id, StringComparer.OrdinalIgnoreCase);
        var spawnTables = bundle.SpawnTables.Select(resource => resource.ToDefinition()).ToDictionary(table => table.Id, StringComparer.OrdinalIgnoreCase);

        ValidateReferences(zones, quests, spawnTables, actors, items);
        return new ContentCatalog(actors, items, zones, quests, spawnTables);
    }

    private static void ValidateReferences(
        IReadOnlyDictionary<string, ZoneDefinition> zones,
        IReadOnlyDictionary<string, QuestDefinition> quests,
        IReadOnlyDictionary<string, SpawnTableDefinition> spawnTables,
        IReadOnlyDictionary<string, ActorDefinition> actors,
        IReadOnlyDictionary<string, ItemDefinition> items)
    {
        foreach (var zone in zones.Values)
        {
            ZoneDefinitionValidator.Validate(zone);

            foreach (var connectedZoneId in zone.ConnectedZoneIds)
            {
                if (!zones.ContainsKey(connectedZoneId))
                {
                    throw new InvalidOperationException($"Zone '{zone.Id}' connects to missing zone '{connectedZoneId}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(zone.SpawnTableId) && !spawnTables.ContainsKey(zone.SpawnTableId))
            {
                throw new InvalidOperationException($"Zone '{zone.Id}' references missing spawn table '{zone.SpawnTableId}'.");
            }
        }

        foreach (var quest in quests.Values)
        {
            foreach (var objective in quest.Objectives)
            {
                if (objective.Kind == QuestObjectiveKind.ClearHostilesInZone && !zones.ContainsKey(objective.TargetZoneId))
                {
                    throw new InvalidOperationException($"Quest '{quest.Id}' references missing target zone '{objective.TargetZoneId}'.");
                }
            }

            if (quest.TurnIn.Kind == QuestTurnInKind.ManualAtZone && !zones.ContainsKey(quest.TurnIn.TargetZoneId))
            {
                throw new InvalidOperationException($"Quest '{quest.Id}' references missing turn-in zone '{quest.TurnIn.TargetZoneId}'.");
            }

            foreach (var reward in quest.Rewards)
            {
                if (reward.Kind == QuestRewardKind.Item && !items.ContainsKey(reward.TargetDefinitionId))
                {
                    throw new InvalidOperationException($"Quest '{quest.Id}' references missing reward item '{reward.TargetDefinitionId}'.");
                }
            }
        }

        foreach (var spawnTable in spawnTables.Values)
        {
            foreach (var actorId in spawnTable.ActorDefinitionIds)
            {
                if (!actors.ContainsKey(actorId))
                {
                    throw new InvalidOperationException($"Spawn table '{spawnTable.Id}' references missing actor '{actorId}'.");
                }
            }
        }
    }
}
