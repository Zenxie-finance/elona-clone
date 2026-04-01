# Quest Turn-In Minimal Design

## Goal

Add the smallest manual quest completion loop that turns dungeon combat into a reusable gameplay cycle:

- accept an already-posted starter quest
- clear the target dungeon objective
- mark the quest as ready to turn in
- return to the target town
- manually claim the reward

This module should stay small, but its interfaces must leave room for stranger Elona-style quests later.

## Reference Notes

This design is based on public descriptions of Elona quest patterns and adjacent variants:

- original request-board style quests include monster hunting, deliveries, escorts, harvest, item requests, and cooked-dish jobs
- quests often differ in turn-in rules, failure conditions, target NPCs, time limits, and over-fulfillment logic
- mobile and storyline quests add multi-step errands, branch choices, rescue goals, item hand-ins, boss kills, and dialogue-driven progression

References used during design:

- https://elona.fandom.com/wiki/Quest_Advice
- https://elona.fandom.com/wiki/Category%3AQuests
- https://elona.fandom.com/wiki/Quests_%28mobile%29

## Freedom Points To Preserve

The system should not assume all quests are "kill everything and auto-complete".

Interfaces should be able to grow toward:

- objective-based quests
  - clear a zone
  - kill a specific actor
  - collect or hold items
  - deliver items
  - reach a zone
  - escort an actor
  - talk to an actor
- different turn-in rules
  - manual claim at a town
  - manual claim at an NPC
  - automatic completion
- different rewards
  - gold
  - items
  - fame / karma / other currencies
- different failure rules
  - time limit
  - target death
  - route failure

## Scope

### In scope

- keep the current starter quest accepted from the beginning
- define quests as combinations of:
  - objectives
  - turn-in policy
  - reward policy
  - failure policy placeholder
- implement one objective evaluator:
  - `ClearHostilesInZone`
- implement one turn-in policy:
  - `ManualAtZone`
- implement one reward policy:
  - `Gold`
- add a manual claim input in the ASCII prototype
- show quest status in the runtime text view

### Out of scope

- full quest log UI
- multiple manual selection menus
- escort behavior
- item-delivery behavior
- dialogue progression
- failure rules beyond placeholders
- dynamic town boards and quest rerolling

## Architecture

The module should stay inside the current separation:

- `Content`
  - richer quest definitions with extensible objective / turn-in / reward / failure descriptors
- `Application`
  - starter quest state creation
- `Simulation`
  - quest progress evaluation after successful player actions
  - manual turn-in action resolution
  - reward application
- `Presentation`
  - add one input for manual claim
  - show quest status and reward preview

Recommended runtime services:

- `QuestProgressService`
  - recomputes objective progress and upgrades quests to `ReadyToTurnIn`
- `QuestTurnInService`
  - validates current zone and turn-in policy
  - applies rewards
  - marks quests completed

## Data Model

### Quest definition

Quest definitions should become composition-based:

- `QuestObjectiveDefinition[]`
- `QuestTurnInDefinition`
- `QuestRewardDefinition[]`
- `QuestFailureDefinition[]`

Only one objective / one turn-in / one reward path is implemented now, but the structure should support later variety.

### Quest state

Quest runtime state should track:

- `Accepted`
- `ReadyToTurnIn`
- `Completed`
- `Failed`

It should also keep per-objective numeric progress storage, even if the first quest only needs boolean-style completion.

## First Implemented Quest

Starter quest:

- title: `Vermin Cleanup`
- objective: clear all hostile actors from `puppy_cave`
- turn-in: manual claim in `vernis`
- reward: `500` gold

Flow:

1. player clears the cave
2. quest status becomes `ReadyToTurnIn`
3. player travels back to `Vernis`
4. player presses the manual claim key
5. quest becomes `Completed`
6. player receives reward

## Input And Presentation

The ASCII runtime should gain one manual turn-in control:

- temporary key: `C`

The text view should show:

- player gold
- quest status
- when relevant, where a ready quest must be turned in

## State Evaluation Rules

- quest progress refresh happens after successful player actions
- once a quest becomes `ReadyToTurnIn`, it should not regress back to `Accepted`
- manual turn-in should fail clearly when:
  - no quest is ready
  - the player is in the wrong zone
  - the turn-in policy is unsupported

## Testing Strategy

Stable regression tests should cover:

- clearing the target dungeon marks the quest ready
- ready quests cannot be turned in from the wrong zone
- turning in from the correct zone grants gold and completes the quest
- quest completion survives save/load
- existing combat, travel, pickup, and save/load tests remain green

## Acceptance Criteria

This module is complete when:

- the starter quest becomes ready after `puppy_cave` is cleared
- the player must return to `Vernis` and manually claim the quest
- the reward is granted only once
- the runtime text view shows enough state to understand quest progress
- the design leaves obvious extension points for escort, delivery, item, dialogue, and branch-style quests later
