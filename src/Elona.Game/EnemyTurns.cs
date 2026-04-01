using ElonaClone.Game;

namespace ElonaClone.Game.Simulation;

public sealed class EnemyTurnResolver
{
    public void ResolveCurrentZoneEnemies(GameSession session, EffectPipeline effectPipeline)
    {
        var player = session.World.GetPlayer(session.PlayerId);
        if (!player.IsAlive)
        {
            return;
        }

        var currentZoneId = session.World.CurrentZoneId;
        var hostiles = session.World.GetActorsInZone(currentZoneId)
            .Where(actor => !actor.IsPlayer && actor.Faction == Faction.Hostile)
            .OrderBy(actor => actor.Position.Y)
            .ThenBy(actor => actor.Position.X)
            .ThenBy(actor => actor.Id.Value)
            .ToArray();

        foreach (var hostile in hostiles)
        {
            if (!player.IsAlive)
            {
                break;
            }

            if (!hostile.IsAlive || !string.Equals(hostile.ZoneId, currentZoneId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ResolveSingleEnemyTurn(session, player, hostile, effectPipeline);
        }
    }

    private static void ResolveSingleEnemyTurn(GameSession session, ActorState player, ActorState hostile, EffectPipeline effectPipeline)
    {
        if (!hostile.IsHostileTo(player))
        {
            return;
        }

        if (IsAdjacent(hostile.Position, player.Position))
        {
            CombatRules.ResolveMeleeAttack(session, effectPipeline, hostile, player);
            return;
        }

        if (TryFindMoveTarget(session, hostile, player, out var moveTarget))
        {
            effectPipeline.MoveActor(hostile, moveTarget);
            effectPipeline.AddMessage(session, $"{hostile.DisplayName} moves to ({moveTarget.X}, {moveTarget.Y}).");
            return;
        }

        effectPipeline.AddMessage(session, $"{hostile.DisplayName} waits.");
    }

    private static bool TryFindMoveTarget(GameSession session, ActorState hostile, ActorState player, out GridPoint moveTarget)
    {
        var zone = session.World.GetZone(hostile.ZoneId);
        foreach (var direction in GetPreferredDirections(hostile.Position, player.Position))
        {
            var candidate = hostile.Position + direction.ToOffset();
            if (!zone.IsWalkable(candidate) || session.World.IsOccupied(hostile.ZoneId, candidate, hostile.Id))
            {
                continue;
            }

            moveTarget = candidate;
            return true;
        }

        moveTarget = hostile.Position;
        return false;
    }

    private static IEnumerable<MoveDirection> GetPreferredDirections(GridPoint origin, GridPoint target)
    {
        var deltaX = target.X - origin.X;
        var deltaY = target.Y - origin.Y;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            if (deltaX != 0)
            {
                yield return deltaX > 0 ? MoveDirection.Right : MoveDirection.Left;
            }

            if (deltaY != 0)
            {
                yield return deltaY > 0 ? MoveDirection.Down : MoveDirection.Up;
            }

            yield break;
        }

        if (deltaY != 0)
        {
            yield return deltaY > 0 ? MoveDirection.Down : MoveDirection.Up;
        }

        if (deltaX != 0)
        {
            yield return deltaX > 0 ? MoveDirection.Right : MoveDirection.Left;
        }
    }

    private static bool IsAdjacent(GridPoint origin, GridPoint target)
    {
        var distance = Math.Abs(origin.X - target.X) + Math.Abs(origin.Y - target.Y);
        return distance == 1;
    }
}
