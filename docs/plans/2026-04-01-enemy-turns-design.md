# Enemy Turns Minimal Design

## Goal

Add the smallest possible enemy-turn module to the current prototype without expanding scope into full world simulation.

This module should make dungeon combat feel reciprocal:

- the player acts
- hostile enemies in the current zone respond
- adjacent enemies attack
- distant enemies move one tile toward the player

The design intentionally avoids pathfinding, off-screen simulation, pet systems, NPC combat, or global turn progression.

## Scope

### In scope

- Only `Hostile` actors participate in enemy turns.
- Only actors in the `current zone` act in real time.
- Enemy turns happen only after a `successful player action`.
- Enemy rules are limited to:
  - attack the player if adjacent
  - move one tile toward the player if not adjacent
  - wait if blocked
- Player HP loss is visible in the existing ASCII runtime view.
- Non-current zones keep a lightweight time-refresh hook for future use.

### Out of scope

- No global simulation across all zones.
- No pathfinding beyond one-step greedy movement.
- No neutral NPC behavior.
- No pets or party members yet.
- No ranged attacks, skills, statuses, or threat logic.
- No full off-screen simulation; other zones refresh only when re-entered in the future.

## Architecture

The implementation should stay inside the existing layer split:

- `Simulation`
  - add enemy-turn resolution
  - keep all combat behavior in pure C#
- `Application`
  - keep world bootstrap unchanged except for any minimal timing metadata needed later
- `Persistence`
  - preserve any new lightweight zone refresh metadata
- `Presentation`
  - only reflect HP/log changes
  - do not own enemy logic

Recommended structure:

- keep `ActionResolver` responsible for player actions
- introduce a focused helper such as `EnemyTurnResolver`
- let `ActionResolver` trigger the enemy phase only after a successful player action

This keeps player input and enemy behavior separate while preserving a simple execution flow.

## Enemy Turn Rules

Enemy decision order for the current zone:

1. Find all alive `Hostile` actors in the current zone.
2. Process them in a deterministic order.
3. For each hostile actor:
   - if dead, skip
   - if not in the current zone anymore, skip
   - if adjacent to the player, perform a melee attack
   - otherwise try to move one step closer using a simple greedy rule
   - if the preferred tile is blocked or invalid, wait

Deterministic order should be stable and easy to test, for example:

- by Y, then X, then actor id

## Movement Rule

Enemy movement should stay intentionally simple:

- calculate the delta from enemy to player
- prefer the axis with the larger absolute distance
- if both axes are tied, prefer the horizontal step first
- try one tile in that direction
- if blocked, try the other axis
- if both fail, wait

This is not pathfinding. It is only enough to make enemies visibly approach the player in open rooms.

## Combat Rule

Enemy attacks use the same minimal melee formula already used by the player:

`damage = max(1, attacker.AttackPower - defender.Defense)`

For this module:

- enemies can damage the player
- player death should be handled safely, even if only as a minimal placeholder state or log outcome
- enemy attacks should write clear combat log messages

If player defeat is not yet fully supported, the first version should still avoid invalid state and log a clear result.

## Off-Screen Zone Refresh Hook

Non-current zones should not receive live turns.

Instead, the design should reserve a lightweight refresh entry point:

- each zone can later track `last refreshed` time
- when a zone is entered, elapsed time can be calculated
- a lightweight zone refresh service can be called

For this module, the hook may be a placeholder as long as the interface direction is clear.

Examples of future lightweight refresh behavior:

- repopulate monsters
- reset simple map state
- advance timers for farm/ranch/property systems

## Data Flow

The intended runtime loop is:

1. player input enters through `Node2d`
2. `InputRouter` maps input to a player `ActionRequest`
3. `ActionResolver` resolves the player action
4. if the player action succeeds:
   - enemy phase runs for hostile actors in the current zone
5. effects are appended to the message log
6. presentation refreshes the current ASCII view

This keeps all game-state mutation in the simulation layer.

## Error Handling

The module should fail safely in these cases:

- no hostile actors in the current zone
- player already dead
- hostile actor becomes dead before its turn
- hostile actor cannot move because tiles are blocked
- hostile actor attempts to act in a different zone

Expected behavior is to skip invalid actors or resolve to `wait`, not throw runtime exceptions.

## Testing Strategy

Stable regression tests should be added for:

- hostile enemy moves toward player after a successful player action
- hostile enemy attacks when adjacent
- hostile enemies in another zone do not act
- blocked hostile enemy waits instead of overlapping actors
- existing player combat, pickup, travel, and save/load tests remain green

Manual acceptance should confirm:

- in `Puppy Cave`, the enemy approaches after the player acts
- when adjacent, the enemy damages the player
- player HP display updates correctly
- no enemy in `Home` or `Vernis` acts while the player is inside another zone

## Acceptance Criteria

This module is complete when:

- current-zone hostile enemies act after successful player actions
- enemy movement is deterministic and testable
- enemy attacks reduce player HP
- no off-screen zones simulate live turns
- build and regression tests pass
- the final change stays scoped to this module only
