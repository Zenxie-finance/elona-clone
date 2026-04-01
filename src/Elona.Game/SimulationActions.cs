using ElonaClone.Game;
using ElonaClone.Game.Application;

namespace ElonaClone.Game.Simulation;

public sealed record ActionRequest(
    EntityId ActorId,
    ActionKind Kind,
    MoveDirection? Direction = null,
    string? TargetZoneId = null,
    EntityId? ItemId = null,
    string? QuestId = null)
{
    public static ActionRequest Move(EntityId actorId, MoveDirection direction) => new(actorId, ActionKind.Move, direction);

    public static ActionRequest Wait(EntityId actorId) => new(actorId, ActionKind.Wait);

    public static ActionRequest Travel(EntityId actorId, string? targetZoneId) => new(actorId, ActionKind.Travel, TargetZoneId: targetZoneId);

    public static ActionRequest PickUp(EntityId actorId, EntityId? itemId = null) => new(actorId, ActionKind.PickUp, ItemId: itemId);

    public static ActionRequest Drop(EntityId actorId, EntityId? itemId = null) => new(actorId, ActionKind.Drop, ItemId: itemId);

    public static ActionRequest TurnInQuest(EntityId actorId, string? questId = null) => new(actorId, ActionKind.TurnInQuest, QuestId: questId);
}

public sealed record SimulationEffect(string Description);

public sealed record ActionResult(bool Success, IReadOnlyList<SimulationEffect> Effects);

public sealed class TurnScheduler
{
    private readonly Queue<EntityId> _turnQueue = new();

    public TurnScheduler()
    {
    }

    public TurnScheduler(IEnumerable<EntityId> actorIds)
    {
        SetTurnOrder(actorIds);
    }

    public EntityId CurrentActorId => _turnQueue.Count == 0
        ? throw new InvalidOperationException("Turn queue is empty.")
        : _turnQueue.Peek();

    public void SetTurnOrder(IEnumerable<EntityId> actorIds)
    {
        _turnQueue.Clear();
        foreach (var actorId in actorIds)
        {
            _turnQueue.Enqueue(actorId);
        }
    }

    public IReadOnlyList<EntityId> Snapshot() => _turnQueue.ToArray();

    public void Advance()
    {
        if (_turnQueue.Count <= 1)
        {
            return;
        }

        var current = _turnQueue.Dequeue();
        _turnQueue.Enqueue(current);
    }
}

public sealed class EffectPipeline
{
    private readonly List<SimulationEffect> _effects = new();

    public void Reset() => _effects.Clear();

    public void AddMessage(GameSession session, string message)
    {
        session.AddMessage(message);
        _effects.Add(new SimulationEffect(message));
    }

    public void MoveActor(ActorState actor, GridPoint position)
    {
        actor.Position = position;
    }

    public void DamageActor(ActorState actor, int damage)
    {
        actor.HitPoints = Math.Max(0, actor.HitPoints - damage);
    }

    public void DefeatActor(ActorState actor)
    {
        actor.HitPoints = 0;
        actor.IsAlive = false;
    }

    public void TravelActor(GameSession session, ActorState actor, string targetZoneId, GridPoint position)
    {
        session.World.LastVisitedZoneId = actor.ZoneId;
        actor.ZoneId = targetZoneId;
        actor.Position = position;
        session.World.CurrentZoneId = targetZoneId;
    }

    public void PickUpItem(ActorState actor, ItemState item)
    {
        if (!actor.InventoryItemIds.Contains(item.Id))
        {
            actor.InventoryItemIds.Add(item.Id);
        }

        item.HolderId = actor.Id;
        item.ZoneId = null;
        item.Position = null;
    }

    public void DropItem(ActorState actor, ItemState item)
    {
        actor.InventoryItemIds.Remove(item.Id);
        item.HolderId = null;
        item.ZoneId = actor.ZoneId;
        item.Position = actor.Position;
    }

    public ActionResult Complete(bool success) => new(success, _effects.ToArray());
}

public sealed class ActionResolver
{
    private readonly EffectPipeline _effectPipeline = new();
    private readonly EnemyTurnResolver _enemyTurnResolver = new();
    private readonly QuestProgressService _questProgressService = new();
    private readonly QuestTurnInService _questTurnInService = new();

    public ActionResult Resolve(GameSession session, ActionRequest request)
    {
        _effectPipeline.Reset();

        if (request.ActorId != session.TurnScheduler.CurrentActorId)
        {
            _effectPipeline.AddMessage(session, "It is not that actor's turn.");
            return _effectPipeline.Complete(false);
        }

        if (!session.World.Actors.TryGetValue(request.ActorId, out var actor))
        {
            _effectPipeline.AddMessage(session, "Unknown actor.");
            return _effectPipeline.Complete(false);
        }

        if (!actor.IsAlive)
        {
            _effectPipeline.AddMessage(session, $"{actor.DisplayName} cannot act because they are defeated.");
            return _effectPipeline.Complete(false);
        }

        return request.Kind switch
        {
            ActionKind.Move => ResolveMove(session, actor, request),
            ActionKind.Wait => ResolveWait(session, actor),
            ActionKind.Travel => ResolveTravel(session, actor, request),
            ActionKind.PickUp => ResolvePickUp(session, actor, request),
            ActionKind.Drop => ResolveDrop(session, actor, request),
            ActionKind.TurnInQuest => ResolveTurnInQuest(session, actor, request),
            _ => _effectPipeline.Complete(false)
        };
    }

    private ActionResult ResolveMove(GameSession session, ActorState actor, ActionRequest request)
    {
        if (request.Direction is null)
        {
            _effectPipeline.AddMessage(session, "Move action is missing a direction.");
            return _effectPipeline.Complete(false);
        }

        var target = actor.Position + request.Direction.Value.ToOffset();
        var zone = session.World.GetZone(actor.ZoneId);

        if (!zone.IsWalkable(target))
        {
            _effectPipeline.AddMessage(session, $"{actor.DisplayName} bumps into a wall.");
            return _effectPipeline.Complete(false);
        }

        var targetActor = session.World.FindAliveActorAt(actor.ZoneId, target, actor.Id);
        if (targetActor is not null)
        {
            return actor.IsHostileTo(targetActor)
                ? ResolveMeleeAttack(session, actor, targetActor)
                : ResolveBlockedByActor(session, actor, targetActor);
        }

        _effectPipeline.MoveActor(actor, target);
        _effectPipeline.AddMessage(session, $"{actor.DisplayName} moves to ({target.X}, {target.Y}).");
        return FinishAction(session, actor, true);
    }

    private ActionResult ResolveWait(GameSession session, ActorState actor)
    {
        _effectPipeline.AddMessage(session, $"{actor.DisplayName} waits.");
        return FinishAction(session, actor, true);
    }

    private ActionResult ResolveBlockedByActor(GameSession session, ActorState actor, ActorState blocker)
    {
        _effectPipeline.AddMessage(session, $"{actor.DisplayName} is blocked by {blocker.DisplayName}.");
        return _effectPipeline.Complete(false);
    }

    private ActionResult ResolveMeleeAttack(GameSession session, ActorState attacker, ActorState defender)
    {
        CombatRules.ResolveMeleeAttack(session, _effectPipeline, attacker, defender);
        return FinishAction(session, attacker, true);
    }

    private ActionResult ResolveTravel(GameSession session, ActorState actor, ActionRequest request)
    {
        var currentZone = session.World.GetZone(actor.ZoneId);
        var targetZoneId = string.IsNullOrWhiteSpace(request.TargetZoneId)
            ? session.World.GetSuggestedTravelZoneId()
            : request.TargetZoneId;

        if (string.IsNullOrWhiteSpace(targetZoneId))
        {
            _effectPipeline.AddMessage(session, "There is nowhere to travel from here.");
            return _effectPipeline.Complete(false);
        }

        if (!currentZone.ConnectedZoneIds.Contains(targetZoneId, StringComparer.OrdinalIgnoreCase))
        {
            _effectPipeline.AddMessage(session, $"No route to '{targetZoneId}'.");
            return _effectPipeline.Complete(false);
        }

        var targetZone = session.World.GetZone(targetZoneId);
        if (!session.World.TryFindClosestOpenTile(targetZone.Id, targetZone.EntryPoint, out var arrivalPoint))
        {
            _effectPipeline.AddMessage(session, $"No open tile is available in {targetZone.DisplayName}.");
            return _effectPipeline.Complete(false);
        }

        _effectPipeline.TravelActor(session, actor, targetZone.Id, arrivalPoint);
        _effectPipeline.AddMessage(session, $"{actor.DisplayName} travels to {targetZone.DisplayName}.");
        return FinishAction(session, actor, true);
    }

    private ActionResult ResolvePickUp(GameSession session, ActorState actor, ActionRequest request)
    {
        var item = session.World.Items.Values.FirstOrDefault(candidate =>
            candidate.HolderId is null
            && string.Equals(candidate.ZoneId, actor.ZoneId, StringComparison.OrdinalIgnoreCase)
            && candidate.Position == actor.Position
            && (request.ItemId is null || candidate.Id == request.ItemId.Value));

        if (item is null)
        {
            _effectPipeline.AddMessage(session, "There is nothing to pick up here.");
            return _effectPipeline.Complete(false);
        }

        _effectPipeline.PickUpItem(actor, item);
        _effectPipeline.AddMessage(session, $"{actor.DisplayName} picks up {item.DisplayName}.");
        return FinishAction(session, actor, true);
    }

    private ActionResult ResolveDrop(GameSession session, ActorState actor, ActionRequest request)
    {
        var itemId = request.ItemId ?? actor.InventoryItemIds.FirstOrDefault();
        if (itemId == default || !session.World.Items.TryGetValue(itemId, out var item) || item.HolderId != actor.Id)
        {
            _effectPipeline.AddMessage(session, "There is nothing to drop.");
            return _effectPipeline.Complete(false);
        }

        _effectPipeline.DropItem(actor, item);
        _effectPipeline.AddMessage(session, $"{actor.DisplayName} drops {item.DisplayName}.");
        return FinishAction(session, actor, true);
    }

    private ActionResult ResolveTurnInQuest(GameSession session, ActorState actor, ActionRequest request)
    {
        var success = _questTurnInService.TryTurnInQuest(session, actor, request.QuestId, _effectPipeline);
        return FinishAction(session, actor, success);
    }

    private ActionResult FinishAction(GameSession session, ActorState actor, bool success)
    {
        if (success)
        {
            session.TurnScheduler.Advance();
            if (actor.IsPlayer && session.World.GetPlayer(session.PlayerId).IsAlive)
            {
                _enemyTurnResolver.ResolveCurrentZoneEnemies(session, _effectPipeline);
                _questProgressService.RefreshQuestStates(session, _effectPipeline);
            }
        }

        return _effectPipeline.Complete(success);
    }
}

internal static class CombatRules
{
    private static readonly ProgressionService ProgressionService = new();

    public static void ResolveMeleeAttack(GameSession session, EffectPipeline effectPipeline, ActorState attacker, ActorState defender)
    {
        var damage = Math.Max(1, attacker.AttackPower - defender.Defense);
        effectPipeline.DamageActor(defender, damage);
        effectPipeline.AddMessage(session, $"{attacker.DisplayName} hits {defender.DisplayName} for {damage} damage.");

        if (defender.HitPoints > 0)
        {
            effectPipeline.AddMessage(session, $"{defender.DisplayName} has {defender.HitPoints}/{defender.MaxHitPoints} HP remaining.");
            return;
        }

        effectPipeline.DefeatActor(defender);
        effectPipeline.AddMessage(session, $"{defender.DisplayName} is defeated.");

        if (defender.IsPlayer)
        {
            effectPipeline.AddMessage(session, "The adventure ends here.");
            return;
        }

        ProgressionService.GrantKillExperience(session, attacker, defender, effectPipeline);

        var droppedItem = TryCreateFixedDrop(session, defender);
        if (droppedItem is not null)
        {
            effectPipeline.AddMessage(session, $"{defender.DisplayName} drops {droppedItem.DisplayName}.");
        }
    }

    private static ItemState? TryCreateFixedDrop(GameSession session, ActorState defeatedActor)
    {
        if (!session.Content.Actors.TryGetValue(defeatedActor.DefinitionId, out var actorDefinition)
            || string.IsNullOrWhiteSpace(actorDefinition.DropItemDefinitionId)
            || !session.Content.Items.TryGetValue(actorDefinition.DropItemDefinitionId, out var itemDefinition))
        {
            return null;
        }

        var droppedItem = new ItemState(
            EntityId.New(),
            itemDefinition.Id,
            itemDefinition.DisplayName,
            itemDefinition.Glyph,
            defeatedActor.ZoneId,
            defeatedActor.Position);

        session.World.Items[droppedItem.Id] = droppedItem;
        return droppedItem;
    }
}
