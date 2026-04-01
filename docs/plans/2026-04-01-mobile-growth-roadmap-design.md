# Mobile-Leaning Growth System Roadmap

## Goal

Define a mobile-leaning growth roadmap for the Elona-inspired prototype that preserves long-term freedom while still being built in small, safe increments.

The roadmap should support:

- immediate and visible player growth
- multiple overlapping progression loops
- future expansion toward pets, allies, food growth, trainers, and property systems
- implementation in many small modules instead of one large risky feature drop

## Reference Notes

This roadmap is based on public descriptions of Elona PC and Elona Mobile growth systems:

- original Elona uses a layered growth model: character experience, attribute growth, skill-by-use, and potential
- Elona Mobile makes growth more explicit and player-facing, with clearer level-up feedback, direct stat impact, and stronger long-term build planning
- skills in mobile are still largely trained by use
- basic stats affect skill caps and combat outcomes
- potential acts as a growth-rate multiplier and long-term limiter
- mobile also emphasizes food, training, allies, and visible stat ceilings

References used:

- https://elona.fandom.com/wiki/Experience
- https://elona.fandom.com/wiki/Skills
- https://elona.fandom.com/wiki/Attributes
- https://elona.fandom.com/wiki/Potential
- https://elona.fandom.com/wiki/Stats_%28Mobile%29
- https://elona.fandom.com/wiki/Skills_%28mobile%29

## Freedom Points To Preserve

The growth system should not be reduced to only "kill monster -> gain level -> bigger numbers".

It should leave room for:

- character level growth
- skill-by-use growth
- stat growth through related actions
- potential-based growth speed
- trainer and town-based growth boosts
- food-based and item-based stat development
- ally and pet growth using the same core structures
- future differentiation between player, ally, and NPC progression

## Recommended Direction

Use a hybrid, mobile-leaning model:

- keep explicit `Level / EXP / level-up rewards`
- keep `skill-by-use` as a parallel line
- make `stats` clearly visible and impactful
- keep `potential` as a data-backed multiplier and future limiter
- do not implement the full original Elona formulas yet

This gives stronger player-facing feedback now, without blocking a later move toward more Elona-like depth.

## Scope Of The Big Task

This roadmap covers the full growth foundation for the early playable loop:

- player level and experience
- level-up rewards
- visible combat-related stats
- first combat skill and skill experience
- first potential layer
- reward hooks from combat and quests
- persistence and display
- future compatibility with allies, trainers, food, and town facilities

It does not yet include:

- full attribute web
- all weapon and life skills
- pet and ally implementation
- trainer UI
- food and cooking growth implementation
- advanced equipment growth
- full mobile stat cap or rarity-grade systems

## Target Architecture

The system should stay modular:

- `Simulation`
  - level, experience, skills, and potentials runtime state
  - reward application
  - use-based growth triggers
- `Application`
  - startup defaults and progression tuning
- `Content`
  - future stat/skill definitions and trainer metadata
- `Persistence`
  - save/load of progression state
- `Presentation`
  - only display and input hooks, no growth rules

Recommended service boundaries:

- `ProgressionService`
  - gain EXP
  - handle level-up
  - apply level rewards
- `SkillGrowthService`
  - award skill XP on use
  - resolve skill level-ups
- `PotentialService`
  - expose potential values and later training hooks

These services should stay separate even if early implementations are small.

## Big Task Split

### Phase 1: Character Level Foundation

Goal:

- add clear level-up progression with visible feedback

Deliverables:

- `Level`
- `CurrentExp`
- `ExpToNextLevel`
- kill EXP reward
- quest turn-in EXP reward
- level-up log messages
- first level-up stat rewards

Why first:

- it gives immediate motivation for combat and quests
- it is the smallest mobile-like growth loop

### Phase 2: Visible Combat Stats

Goal:

- formalize player growth outputs beyond raw HP

Deliverables:

- visible core stats in runtime state
- first stat growth mapping
- level-up reward table for:
  - Max HP
  - Attack
  - Defense
- display in ASCII UI

### Phase 3: First Skill-By-Use Loop

Goal:

- make actions improve a skill, not only character level

Deliverables:

- first combat skill: `Melee`
- skill XP from successful melee hits
- melee skill level
- visible skill readout in UI
- simple relationship between skill level and combat output

### Phase 4: Potential Layer

Goal:

- add long-term growth-rate control without implementing every advanced formula

Deliverables:

- per-skill potential field
- per-stat potential placeholder
- growth multiplier applied to skill XP
- stable data model for future trainer and food systems

### Phase 5: Level-Up Rewards Expansion

Goal:

- make level-ups feel more like a real build choice

Deliverables:

- level-up reward points or auto-allocation model
- explicit stat reward event
- preparation for future manual allocation

For the first implementation, this can still stay auto-applied.

### Phase 6: Training Sources Beyond Combat

Goal:

- prepare mobile-like multi-source growth

Deliverables:

- hooks for:
  - quest EXP
  - trainer growth
  - food growth
  - rest/sleep growth
  - training machine effects

Only hooks and interfaces are needed first; not all sources must be playable immediately.

### Phase 7: Ally/Pet Compatibility

Goal:

- ensure player growth code can later be reused by allies

Deliverables:

- actor progression data not hardcoded to player only
- room for ally-specific caps and scaling
- no rewrite required when party systems arrive

## Small Implementation Tasks

These are the small modules that should be built one by one.

### Task 1

`Level / EXP / level-up skeleton`

- add progression state to actor
- add EXP reward hooks for kills and quest turn-in
- add first level-up thresholds
- show level and EXP in UI

### Task 2

`Level-up stat rewards`

- increase Max HP / Attack / Defense on level-up
- restore HP on level-up for easier testing
- add tests for repeated EXP gain and multiple levels

### Task 3

`First skill: Melee`

- add Melee skill state
- grant skill XP on successful melee attacks
- show Melee level in UI
- keep formulas intentionally simple

### Task 4

`Potential placeholder`

- add Melee potential
- make skill XP gain scale with potential
- keep tuning simple and documented

### Task 5

`Quest EXP integration`

- add explicit quest EXP reward path
- separate gold rewards from EXP rewards
- preserve flexibility for later reward bundles

### Task 6

`Progression save/load hardening`

- persist level, EXP, skills, and potential
- add regression coverage for progression round-trip

### Task 7

`Growth feedback polish`

- improve log messages for EXP gain, skill gain, and level-up
- keep presentation text-only

### Task 8

`Trainer / food / machine hooks`

- define extension interfaces
- do not implement their full gameplay yet

## Recommended Build Order

The safest order is:

1. Task 1
2. Task 2
3. Task 5
4. Task 3
5. Task 4
6. Task 6
7. Task 7
8. Task 8

Reason:

- first make progression visible
- then connect quest rewards
- then add use-based skill growth
- then add multiplier complexity

## Data Model Direction

The long-lived runtime model should be able to grow toward something like:

- `ActorProgressionState`
  - `Level`
  - `CurrentExp`
  - `ExpToNextLevel`
  - `Stats`
  - `Skills`
  - `Potentials`

But the first small modules can stage this in simpler fields before a later cleanup refactor.

## Testing Strategy

Each small task should add its own stable regression tests.

Core checks to accumulate over time:

- kill grants EXP
- quest completion grants EXP
- level-up occurs at the right threshold
- level-up applies stat rewards once
- melee use grants melee skill XP
- potential changes skill XP gain
- progression survives save/load

Manual acceptance should remain simple:

- kill an enemy
- see EXP rise
- level up
- complete a quest
- see additional reward and growth feedback

## Acceptance Criteria For The Big Task

The growth foundation is considered complete when:

- combat and quests both feed visible progression
- level-ups have clear gameplay impact
- at least one skill grows through use
- potential exists as a real growth input
- the data model can extend to trainers, food, allies, and pets
- each slice was delivered in small, testable modules rather than one large drop
