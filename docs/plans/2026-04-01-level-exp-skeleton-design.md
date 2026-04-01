# Level / EXP Skeleton Design

## Goal

Add the smallest visible level progression loop for the current prototype:

- hostile kills grant EXP
- quest turn-in can grant EXP
- EXP can raise the actor's level
- the runtime view shows level progress clearly

This is the first slice of the mobile-leaning growth roadmap. It should make progression visible without pulling in the next layers yet.

## Reference Notes

This design is based on public descriptions of original Elona and Elona Mobile progression:

- original Elona treats level as only one layer of growth, with lots of EXP coming from skill use rather than pure kills
- Elona Mobile makes progression more visible to the player, while still keeping skill-by-use and potential as parallel systems
- mobile skills still grow by use, and leveling gives explicit player-facing feedback and training resources
- mobile stats and skills both have potential, but that complexity should be deferred for now

References used during design:

- https://elona.fandom.com/wiki/Experience
- https://elona.fandom.com/wiki/Skills_%28mobile%29
- https://elona.fandom.com/wiki/Stats_%28Mobile%29
- https://elona.fandom.com/wiki/Potential

## Freedom Points To Preserve

This module should not lock the project into a pure "kill monster -> gain level -> bigger numbers" structure.

The first implementation should still leave room for:

- later skill-by-use growth
- later potential-based growth modifiers
- growth rewards from trainers, food, rest, and town facilities
- quest rewards that include currencies, items, EXP, or mixed bundles
- ally and pet progression using the same core runtime shape

## Approaches Considered

### Approach A: Direct scalar fields on `ActorState`

- add `Level`, `CurrentExp`, and `ExpToNextLevel` directly to the actor
- fastest to ship
- easiest to display
- but grows awkwardly once skills and potentials are added

### Approach B: Small nested progression state

- add an `ActorProgressionState` owned by `ActorState`
- keep level and EXP together now
- reserve a clear place for later skills and potentials
- slightly more plumbing in save/load, but a cleaner long-term shape

### Approach C: Full mobile-style progression bundle now

- add level, skill XP, potential, and stat reward logic together
- closest to long-term target
- too large and risky for the current task

## Recommended Direction

Use **Approach B**.

This task should introduce a small `ActorProgressionState` with:

- `Level`
- `CurrentExp`
- computed `ExpToNextLevel`

It keeps the current module small, but gives later tasks an obvious place to attach:

- stat rewards
- skill progression
- potential
- ally growth

## Scope

### In scope

- add actor progression state with `Level` and `CurrentExp`
- add a simple EXP curve for early levels
- grant EXP from hostile kills
- grant EXP from quest turn-in
- allow overflow EXP to carry across level-ups
- log EXP gain and level-up messages
- show player level and EXP in the ASCII runtime
- persist progression state through save/load

### Out of scope

- level-up stat rewards
- Melee skill growth
- potential
- trainer or food growth
- manual stat allocation
- UI menus beyond the existing ASCII output

## Architecture

The change should stay inside the current layer split:

- `Simulation`
  - actor progression runtime state
  - EXP gain and level-up resolution
- `Content`
  - actor kill EXP reward
  - quest EXP reward
- `Persistence`
  - save/load progression state
- `Presentation`
  - display level and EXP only

Recommended runtime service:

- `ProgressionService`
  - grant EXP to an actor
  - resolve one or more level-ups
  - emit log messages through the existing effect pipeline

This keeps reward entry points separate from the actual level-up logic.

## Data Model

### Actor progression state

Add a nested runtime state:

- `Level`
- `CurrentExp`

`ExpToNextLevel` should be derived from the current level rather than stored redundantly.

### Reward definitions

Reserve explicit reward sources now:

- `ActorDefinition.KillExperienceReward`
- `QuestRewardKind.Experience`

That keeps quest EXP separate from gold even in this first slice, which matches the roadmap direction.

### Current EXP ownership rule

For the current prototype, `ProgressionService` accepts any actor passed in as the EXP receiver.

That means:

- kill EXP currently belongs to the attacking actor
- quest EXP currently belongs to the actor turning in the quest

This is only a temporary explicit rule for the single-actor prototype. It is not the final ally / pet / party distribution policy.

## EXP Curve

Keep the first curve intentionally simple and easy to test:

- level 1 -> 2 requires `100 EXP`
- early levels use `level * 100`

This is not meant to match full Elona formulas. It is only a visible skeleton that later modules can rebalance.

## Starter Tuning

Starter values should make the first loop feel rewarding:

- `Putit` grants a small kill reward
- `Vermin Cleanup` grants an explicit quest EXP reward

The combined first dungeon loop should be close to or just enough for the first level-up, so the player gets immediate feedback from the prototype loop.

## Data Flow

1. Player defeats a hostile actor.
2. Combat code asks `ProgressionService` to grant kill EXP.
3. Service updates the attacker's progression state and emits log messages.
4. If a threshold is crossed, level increases and overflow EXP carries over.
5. When a quest is turned in, the quest reward flow can also call the same service for EXP rewards.

## Error Handling

The module should fail safely when:

- a defeated actor definition has no EXP reward
- a quest has no EXP reward
- EXP reward is zero or negative
- an actor gains enough EXP for multiple levels at once
- a `v1` save is restored without progression fields

Expected behavior is to skip zero-value rewards and keep the actor state consistent.

## Save Snapshot Notes

- current save snapshot version: `v2`
- `v1` saves restore with safe defaults for progression:
  - `Level = 1`
  - `CurrentExp = 0`
  - `RewardExperience = 0`
- unknown future save versions should fail loudly instead of being silently interpreted as the current shape

## Testing Strategy

Stable regression tests should cover:

- a hostile kill grants EXP to the player
- a hostile kill below threshold does not level the player
- quest turn-in grants EXP
- EXP can cross a level threshold and carry overflow correctly
- progression survives save/load
- existing combat, quest, and save/load tests remain green

## Acceptance Criteria

This module is complete when:

- the player starts at level 1 with visible EXP state
- defeating a hostile increases EXP
- turning in the starter quest increases EXP
- level-up happens at the configured threshold
- the ASCII runtime shows level and current EXP progress
- save/load keeps progression intact
- the design still leaves room for later stat rewards, skill-by-use, and potential
