using ElonaClone.Game.Content;
using ElonaClone.Game.Simulation;

namespace ElonaClone.Game.Application;

public static class ProgressionCurves
{
    public static int GetExpToNextLevel(int level)
    {
        return Math.Max(100, Math.Max(1, level) * 100);
    }
}

public sealed class ProgressionService
{
    // Current temporary rule: progression can be awarded to any actor that is explicitly
    // passed in as the receiver. Party/pet sharing is deferred to a later module.
    public void GrantKillExperience(GameSession session, ActorState actor, ActorState defeatedActor, EffectPipeline effectPipeline)
    {
        if (!session.Content.Actors.TryGetValue(defeatedActor.DefinitionId, out var definition))
        {
            return;
        }

        GrantExperience(session, actor, definition.KillExperienceReward, $"defeating {defeatedActor.DisplayName}", effectPipeline);
    }

    public void GrantQuestRewardExperience(GameSession session, ActorState actor, int amount, string questTitle, EffectPipeline effectPipeline)
    {
        GrantExperience(session, actor, amount, $"completing {questTitle}", effectPipeline);
    }

    public void GrantExperience(GameSession session, ActorState actor, int amount, string sourceDescription, EffectPipeline effectPipeline)
    {
        if (amount <= 0)
        {
            return;
        }

        actor.Progression.CurrentExp += amount;
        effectPipeline.AddMessage(session, $"{actor.DisplayName} gains {amount} EXP from {sourceDescription}.");

        while (actor.Progression.CurrentExp >= actor.Progression.ExpToNextLevel)
        {
            var threshold = actor.Progression.ExpToNextLevel;
            actor.Progression.CurrentExp -= threshold;
            actor.Progression.Level++;
            effectPipeline.AddMessage(session, $"{actor.DisplayName} reaches level {actor.Progression.Level}.");
        }
    }
}
