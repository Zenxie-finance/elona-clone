using ElonaClone.Game;
using ElonaClone.Game.Content;
using ElonaClone.Game.Simulation;

namespace ElonaClone.Game.Application;

public sealed class QuestProgressService
{
    public void RefreshQuestStates(GameSession session, EffectPipeline effectPipeline)
    {
        foreach (var questState in session.World.Quests.Values.OrderBy(quest => quest.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (questState.IsCompleted || questState.IsFailed)
            {
                continue;
            }

            if (!session.Content.Quests.TryGetValue(questState.Id, out var questDefinition))
            {
                throw new InvalidOperationException($"Missing quest definition '{questState.Id}'.");
            }

            foreach (var objective in questDefinition.Objectives)
            {
                questState.ObjectiveProgressValues[objective.Id] = EvaluateObjectiveProgress(session, objective);
            }

            if (questState.Status == QuestStatus.Accepted && AreAllObjectivesSatisfied(questState, questDefinition))
            {
                questState.Status = QuestStatus.ReadyToTurnIn;
                effectPipeline.AddMessage(session, $"Quest ready to turn in: {questState.Title}.");
            }
        }
    }

    private static bool AreAllObjectivesSatisfied(QuestState questState, QuestDefinition definition)
    {
        return definition.Objectives.All(objective =>
            questState.ObjectiveProgressValues.TryGetValue(objective.Id, out var progress)
            && progress >= Math.Max(1, objective.RequiredAmount));
    }

    private static int EvaluateObjectiveProgress(GameSession session, QuestObjectiveDefinition objective)
    {
        return objective.Kind switch
        {
            QuestObjectiveKind.ClearHostilesInZone => session.World.GetActorsInZone(objective.TargetZoneId).Any(actor => actor.Faction == Faction.Hostile)
                ? 0
                : 1,
            QuestObjectiveKind.DefeatActorByDefinition => throw new NotSupportedException("DefeatActorByDefinition objectives are reserved for a later module."),
            QuestObjectiveKind.CollectItemByDefinition => throw new NotSupportedException("CollectItemByDefinition objectives are reserved for a later module."),
            QuestObjectiveKind.DeliverItemByDefinition => throw new NotSupportedException("DeliverItemByDefinition objectives are reserved for a later module."),
            QuestObjectiveKind.ReachZone => throw new NotSupportedException("ReachZone objectives are reserved for a later module."),
            QuestObjectiveKind.EscortActorToZone => throw new NotSupportedException("EscortActorToZone objectives are reserved for a later module."),
            QuestObjectiveKind.TalkToActor => throw new NotSupportedException("TalkToActor objectives are reserved for a later module."),
            _ => throw new InvalidOperationException($"Unsupported quest objective kind '{objective.Kind}'.")
        };
    }
}

public sealed class QuestTurnInService
{
    private readonly ProgressionService _progressionService = new();

    public bool TryTurnInQuest(GameSession session, ActorState actor, string? questId, EffectPipeline effectPipeline)
    {
        if (!actor.IsPlayer)
        {
            effectPipeline.AddMessage(session, "Only the player can claim quests.");
            return false;
        }

        var readyQuests = session.World.Quests.Values
            .Where(quest => quest.IsReadyToTurnIn)
            .OrderBy(quest => quest.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(questId))
        {
            var requestedQuest = readyQuests.FirstOrDefault(quest => string.Equals(quest.Id, questId, StringComparison.OrdinalIgnoreCase));
            if (requestedQuest is null)
            {
                effectPipeline.AddMessage(session, "There is no quest ready to turn in.");
                return false;
            }

            if (!session.Content.Quests.TryGetValue(requestedQuest.Id, out var requestedDefinition))
            {
                throw new InvalidOperationException($"Missing quest definition '{requestedQuest.Id}'.");
            }

            if (!CanTurnInAtCurrentLocation(actor, requestedDefinition, effectPipeline, session))
            {
                return false;
            }

            ApplyRewards(session, actor, requestedQuest, requestedDefinition, effectPipeline);
            requestedQuest.Status = QuestStatus.Completed;
            effectPipeline.AddMessage(session, $"Quest completed: {requestedQuest.Title}.");
            return true;
        }

        var questState = readyQuests.FirstOrDefault(quest =>
            session.Content.Quests.TryGetValue(quest.Id, out var definition)
            && IsTurnInLocationValid(actor, definition));

        if (questState is null)
        {
            effectPipeline.AddMessage(session, readyQuests.Length == 0
                ? "There is no quest ready to turn in."
                : "There is no quest ready to turn in here.");
            return false;
        }

        if (!session.Content.Quests.TryGetValue(questState.Id, out var questDefinition))
        {
            throw new InvalidOperationException($"Missing quest definition '{questState.Id}'.");
        }

        if (!CanTurnInAtCurrentLocation(actor, questDefinition, effectPipeline, session))
        {
            return false;
        }

        ApplyRewards(session, actor, questState, questDefinition, effectPipeline);
        questState.Status = QuestStatus.Completed;
        effectPipeline.AddMessage(session, $"Quest completed: {questState.Title}.");
        return true;
    }

    private static bool CanTurnInAtCurrentLocation(ActorState actor, QuestDefinition definition, EffectPipeline effectPipeline, GameSession session)
    {
        return definition.TurnIn.Kind switch
        {
            QuestTurnInKind.ManualAtZone => ValidateTurnInZone(actor, definition.TurnIn.TargetZoneId, effectPipeline, session),
            QuestTurnInKind.ManualAtActor => throw new NotSupportedException("ManualAtActor quest turn-in is reserved for a later module."),
            QuestTurnInKind.Automatic => throw new NotSupportedException("Automatic quest turn-in is reserved for a later module."),
            _ => throw new InvalidOperationException($"Unsupported quest turn-in kind '{definition.TurnIn.Kind}'.")
        };
    }

    private static bool IsTurnInLocationValid(ActorState actor, QuestDefinition definition)
    {
        return definition.TurnIn.Kind switch
        {
            QuestTurnInKind.ManualAtZone => string.Equals(actor.ZoneId, definition.TurnIn.TargetZoneId, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool ValidateTurnInZone(ActorState actor, string targetZoneId, EffectPipeline effectPipeline, GameSession session)
    {
        if (string.Equals(actor.ZoneId, targetZoneId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        effectPipeline.AddMessage(session, $"This quest must be turned in at {targetZoneId}.");
        return false;
    }

    private void ApplyRewards(
        GameSession session,
        ActorState actor,
        QuestState questState,
        QuestDefinition definition,
        EffectPipeline effectPipeline)
    {
        foreach (var reward in definition.Rewards)
        {
            switch (reward.Kind)
            {
                case QuestRewardKind.Gold:
                    actor.Gold += reward.Amount;
                    effectPipeline.AddMessage(session, $"{actor.DisplayName} receives {reward.Amount} gold.");
                    break;
                case QuestRewardKind.Experience:
                    _progressionService.GrantQuestRewardExperience(session, actor, reward.Amount, questState.Title, effectPipeline);
                    break;
                case QuestRewardKind.Item:
                    throw new NotSupportedException("Item rewards are reserved for a later module.");
                case QuestRewardKind.Fame:
                    throw new NotSupportedException("Fame rewards are reserved for a later module.");
                case QuestRewardKind.Karma:
                    throw new NotSupportedException("Karma rewards are reserved for a later module.");
                default:
                    throw new InvalidOperationException($"Unsupported quest reward kind '{reward.Kind}'.");
            }
        }

        if (definition.Rewards.Length == 0)
        {
            if (questState.RewardGold > 0)
            {
                actor.Gold += questState.RewardGold;
                effectPipeline.AddMessage(session, $"{actor.DisplayName} receives {questState.RewardGold} gold.");
            }

            if (questState.RewardExperience > 0)
            {
                _progressionService.GrantQuestRewardExperience(session, actor, questState.RewardExperience, questState.Title, effectPipeline);
            }
        }
    }
}
