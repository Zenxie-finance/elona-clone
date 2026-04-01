using ElonaClone.Game;
using ElonaClone.Game.Application;
using ElonaClone.Game.Content;
using ElonaClone.Game.Persistence;
using ElonaClone.Game.Simulation;
using System.Text.Json.Nodes;

var tests = new (string Name, Action Body)[]
{
    ("bootstrap_world", BootstrapWorld),
    ("player_starts_with_level_one_and_zero_exp", PlayerStartsWithLevelOneAndZeroExp),
    ("player_turn_stays_playable_with_spawned_enemy", PlayerTurnStaysPlayableWithSpawnedEnemy),
    ("killing_hostile_grants_exp_without_level_up", KillingHostileGrantsExpWithoutLevelUp),
    ("kill_exp_still_goes_to_the_attacking_actor", KillExpStillGoesToTheAttackingActor),
    ("clearing_target_zone_marks_quest_ready_to_turn_in", ClearingTargetZoneMarksQuestReadyToTurnIn),
    ("ready_quest_cannot_turn_in_from_wrong_zone", ReadyQuestCannotTurnInFromWrongZone),
    ("manual_turn_in_awards_gold_and_completes_quest", ManualTurnInAwardsGoldAndCompletesQuest),
    ("quest_turn_in_grants_exp_and_levels_player", QuestTurnInGrantsExpAndLevelsPlayer),
    ("v1_save_without_progression_fields_restores_to_level_one_and_zero_exp", V1SaveWithoutProgressionFieldsRestoresToLevelOneAndZeroExp),
    ("v1_save_without_quest_reward_experience_restores_quest_exp_reward_to_zero", V1SaveWithoutQuestRewardExperienceRestoresQuestExpRewardToZero),
    ("v2_save_round_trip_keeps_level_exp_and_quest_reward_experience", V2SaveRoundTripKeepsLevelExpAndQuestRewardExperience),
    ("completed_quest_survives_save_load", CompletedQuestSurvivesSaveLoad),
    ("progression_survives_save_load", ProgressionSurvivesSaveLoad),
    ("hostile_moves_toward_player_after_player_action", HostileMovesTowardPlayerAfterPlayerAction),
    ("hostile_attacks_when_adjacent_after_player_action", HostileAttacksWhenAdjacentAfterPlayerAction),
    ("blocked_hostile_waits_instead_of_overlapping", BlockedHostileWaitsInsteadOfOverlapping),
    ("hostile_in_other_zone_does_not_act", HostileInOtherZoneDoesNotAct),
    ("failed_player_action_does_not_trigger_enemy_turn", FailedPlayerActionDoesNotTriggerEnemyTurn),
    ("defeated_player_cannot_act", DefeatedPlayerCannotAct),
    ("move_into_hostile_triggers_melee_attack", MoveIntoHostileTriggersMeleeAttack),
    ("killing_hostile_spawns_fixed_drop", KillingHostileSpawnsFixedDrop),
    ("move_into_non_hostile_is_blocked", MoveIntoNonHostileIsBlocked),
    ("move_and_wall_resolution", MoveAndWallResolution),
    ("pick_up_and_drop", PickUpAndDrop),
    ("travel_uses_open_tile_when_entry_is_occupied", TravelUsesOpenTileWhenEntryIsOccupied),
    ("travel_and_save_round_trip", TravelAndSaveRoundTrip),
    ("zone_assembler_rejects_wall_entry_point", ZoneAssemblerRejectsWallEntryPoint)
};

var failures = new List<string>();
foreach (var (name, body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {name}");
        Console.WriteLine(ex);
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Test failures:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($" - {failure}");
    }

    Environment.ExitCode = 1;
}

static void BootstrapWorld()
{
    var session = BuildSession();
    Assert(session.World.Zones.Count == 3, "Expected three starter zones.");
    Assert(session.World.CurrentZoneId == "home", "Expected the session to start at home.");
    Assert(session.World.Quests.ContainsKey("vermin_cleanup"), "Expected a starter quest.");
    Assert(session.World.GetPlayer(session.PlayerId).InventoryItemIds.Count == 1, "Expected a ration in the starting inventory.");
    Assert(session.World.GetActorsInZone("puppy_cave").Any(actor => !actor.IsPlayer), "Expected the starter cave to contain at least one spawned enemy.");
}

static void PlayerStartsWithLevelOneAndZeroExp()
{
    var session = BuildSession();
    var player = session.World.GetPlayer(session.PlayerId);

    Assert(player.Level == 1, "Expected the player to start at level 1.");
    Assert(player.CurrentExp == 0, "Expected the player to start with zero EXP.");
    Assert(player.ExpToNextLevel == 100, "Expected the first level threshold to be 100 EXP.");
}

static void PlayerTurnStaysPlayableWithSpawnedEnemy()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    var firstWait = resolver.Resolve(session, ActionRequest.Wait(player.Id));
    var secondWait = resolver.Resolve(session, ActionRequest.Wait(player.Id));

    Assert(firstWait.Success, "Expected the player to be able to act on the first turn.");
    Assert(secondWait.Success, "Expected the player to keep acting even when non-player actors exist elsewhere in the world.");
    Assert(session.TurnScheduler.CurrentActorId == session.PlayerId, "Expected the current turn to remain on the player until NPC turns are implemented.");
}

static void KillingHostileGrantsExpWithoutLevelUp()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(2, 1));
    hostile.HitPoints = 1;

    var attackResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));

    Assert(attackResult.Success, "Expected the finishing attack to succeed.");
    Assert(player.Level == 1, "Expected a single low-level hostile kill not to level the player immediately.");
    Assert(player.CurrentExp == 25, "Expected defeating Putit to grant the configured kill EXP.");
    Assert(session.MessageLog.Any(message => message.Contains("gains 25 EXP", StringComparison.OrdinalIgnoreCase)), "Expected the log to mention the EXP reward from the kill.");
}

static void KillExpStillGoesToTheAttackingActor()
{
    var session = BuildSession();
    var effectPipeline = new EffectPipeline();
    var progressionService = new ProgressionService();
    var attacker = SpawnCustomHostileActor(session, "home", "Hunter", new GridPoint(1, 1), attackPower: 4, defense: 0);
    attacker.Progression.Level = 3;
    attacker.Progression.CurrentExp = 40;
    var defeated = SpawnDefinedActor(session, "putit", "home", new GridPoint(2, 1));

    progressionService.GrantKillExperience(session, attacker, defeated, effectPipeline);

    Assert(attacker.Level == 3, "Expected the attacking actor's level to stay unchanged below the next threshold.");
    Assert(attacker.CurrentExp == 65, "Expected kill EXP to be granted to the attacking actor.");
}

static void ClearingTargetZoneMarksQuestReadyToTurnIn()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var quest = session.World.Quests["vermin_cleanup"];
    var hostile = session.World.GetActorsInZone("puppy_cave").Single(actor => !actor.IsPlayer);
    hostile.Position = new GridPoint(2, 1);
    hostile.HitPoints = 1;

    resolver.Resolve(session, ActionRequest.Travel(player.Id, "vernis"));
    resolver.Resolve(session, ActionRequest.Travel(player.Id, "puppy_cave"));

    player.Position = new GridPoint(2, 2);
    var attackResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));

    Assert(attackResult.Success, "Expected the final attack in the quest zone to succeed.");
    Assert(quest.Status == QuestStatus.ReadyToTurnIn, "Expected clearing the quest zone to mark the quest ready to turn in.");
    Assert(session.MessageLog.Any(message => message.Contains("Quest ready to turn in", StringComparison.OrdinalIgnoreCase)), "Expected the log to mention the quest becoming ready.");
}

static void ReadyQuestCannotTurnInFromWrongZone()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    resolver.Resolve(session, ActionRequest.Travel(player.Id, "home"));
    var turnInResult = resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id));

    Assert(!turnInResult.Success, "Expected manual turn-in to fail outside the target town.");
    Assert(session.World.Quests["vermin_cleanup"].Status == QuestStatus.ReadyToTurnIn, "Expected the quest to remain ready after a failed turn-in attempt.");
    Assert(player.Gold == 0, "Expected no reward to be granted from an invalid turn-in location.");
}

static void ManualTurnInAwardsGoldAndCompletesQuest()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    var turnInResult = resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id));

    Assert(turnInResult.Success, "Expected the ready quest to turn in successfully from Vernis.");
    Assert(session.World.Quests["vermin_cleanup"].Status == QuestStatus.Completed, "Expected the quest to be marked completed after manual turn-in.");
    Assert(player.Gold == 500, "Expected the player to receive the configured gold reward.");
    Assert(session.MessageLog.Any(message => message.Contains("Quest completed: Vermin Cleanup", StringComparison.OrdinalIgnoreCase)), "Expected the log to record quest completion.");
}

static void QuestTurnInGrantsExpAndLevelsPlayer()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    var turnInResult = resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id));

    Assert(turnInResult.Success, "Expected the quest turn-in to succeed.");
    Assert(player.Level == 2, "Expected the combined first loop rewards to level the player up once.");
    Assert(player.CurrentExp == 15, "Expected overflow EXP to carry into the next level threshold.");
    Assert(session.MessageLog.Any(message => message.Contains("gains 90 EXP", StringComparison.OrdinalIgnoreCase)), "Expected the log to mention the quest EXP reward.");
    Assert(session.MessageLog.Any(message => message.Contains("reaches level 2", StringComparison.OrdinalIgnoreCase)), "Expected the log to mention the level-up.");
}

static void V1SaveWithoutProgressionFieldsRestoresToLevelOneAndZeroExp()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    Assert(resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id)).Success, "Expected setup quest turn-in to succeed.");

    var saveLoad = new SaveLoadService();
    var legacyJson = BuildLegacyV1Json(saveLoad, session, removeQuestRewardExperience: false);
    var restored = saveLoad.RestoreSnapshot(saveLoad.Deserialize(legacyJson), BuildCatalog());
    var restoredPlayer = restored.World.GetPlayer(restored.PlayerId);

    Assert(restoredPlayer.Level == 1, "Expected v1 saves without progression fields to restore the default player level.");
    Assert(restoredPlayer.CurrentExp == 0, "Expected v1 saves without progression fields to restore zero EXP.");
    Assert(restoredPlayer.ExpToNextLevel == 100, "Expected restored v1 saves to use the level-1 threshold.");
}

static void V1SaveWithoutQuestRewardExperienceRestoresQuestExpRewardToZero()
{
    var session = BuildSession();
    var saveLoad = new SaveLoadService();
    var legacyJson = BuildLegacyV1Json(saveLoad, session, removeQuestRewardExperience: true);
    var restored = saveLoad.RestoreSnapshot(saveLoad.Deserialize(legacyJson), BuildCatalog());

    Assert(restored.World.Quests["vermin_cleanup"].RewardExperience == 0, "Expected v1 saves without quest EXP reward fields to restore a safe default of zero.");
}

static void V2SaveRoundTripKeepsLevelExpAndQuestRewardExperience()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    Assert(resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id)).Success, "Expected setup quest turn-in to succeed.");

    var saveLoad = new SaveLoadService();
    var snapshot = saveLoad.CreateSnapshot(session);
    var restored = saveLoad.RestoreSnapshot(saveLoad.Deserialize(saveLoad.Serialize(snapshot)), BuildCatalog());
    var restoredPlayer = restored.World.GetPlayer(restored.PlayerId);

    Assert(snapshot.Version == SaveLoadService.CurrentSnapshotVersion, "Expected newly created saves to use the current snapshot version.");
    Assert(restoredPlayer.Level == 2, "Expected v2 save round-trip to preserve the player level.");
    Assert(restoredPlayer.CurrentExp == 15, "Expected v2 save round-trip to preserve EXP overflow.");
    Assert(restored.World.Quests["vermin_cleanup"].RewardExperience == 90, "Expected v2 save round-trip to preserve quest EXP rewards.");
}

static void CompletedQuestSurvivesSaveLoad()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var turnInResult = resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id));

    Assert(turnInResult.Success, "Expected the quest turn-in to succeed before save/load.");

    var saveLoad = new SaveLoadService();
    var snapshot = saveLoad.CreateSnapshot(session);
    var restored = saveLoad.RestoreSnapshot(saveLoad.Deserialize(saveLoad.Serialize(snapshot)), BuildCatalog());

    Assert(restored.World.Quests["vermin_cleanup"].Status == QuestStatus.Completed, "Expected completed quest status to survive save/load.");
    Assert(restored.World.GetPlayer(restored.PlayerId).Gold == 500, "Expected rewarded gold to survive save/load.");
}

static void ProgressionSurvivesSaveLoad()
{
    var session = BuildReadyToTurnInQuestSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    var turnInResult = resolver.Resolve(session, ActionRequest.TurnInQuest(player.Id));
    Assert(turnInResult.Success, "Expected the quest turn-in to succeed before save/load.");
    Assert(player.Level == 2, "Expected the pre-save player state to include the first level-up.");

    var saveLoad = new SaveLoadService();
    var snapshot = saveLoad.CreateSnapshot(session);
    var restored = saveLoad.RestoreSnapshot(saveLoad.Deserialize(saveLoad.Serialize(snapshot)), BuildCatalog());
    var restoredPlayer = restored.World.GetPlayer(restored.PlayerId);

    Assert(restoredPlayer.Level == 2, "Expected the restored player level to match the saved progression.");
    Assert(restoredPlayer.CurrentExp == 15, "Expected the restored player EXP overflow to survive save/load.");
    Assert(restoredPlayer.ExpToNextLevel == 200, "Expected the restored next-level threshold to match level 2.");
}

static void HostileMovesTowardPlayerAfterPlayerAction()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(1, 1));

    var waitResult = resolver.Resolve(session, ActionRequest.Wait(player.Id));

    Assert(waitResult.Success, "Expected waiting to succeed and trigger the enemy phase.");
    Assert(hostile.Position == new GridPoint(2, 1), "Expected the hostile to take one greedy step toward the player.");
    Assert(session.MessageLog.Any(message => message.Contains("moves to (2, 1)", StringComparison.OrdinalIgnoreCase)), "Expected the log to record the hostile movement.");
}

static void HostileAttacksWhenAdjacentAfterPlayerAction()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(2, 1));

    var waitResult = resolver.Resolve(session, ActionRequest.Wait(player.Id));

    Assert(waitResult.Success, "Expected waiting to succeed before the enemy responds.");
    Assert(hostile.Position == new GridPoint(2, 1), "Expected an adjacent hostile to attack instead of moving.");
    Assert(player.HitPoints == 19, "Expected the hostile attack to reduce the player's HP by the simple melee formula.");
    Assert(session.MessageLog.Any(message => message.Contains("Putit hits Adventurer", StringComparison.OrdinalIgnoreCase)), "Expected the combat log to include the hostile attack.");
}

static void BlockedHostileWaitsInsteadOfOverlapping()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(1, 1));

    SpawnNeutralActor(session, "home", "Guard A", new GridPoint(2, 1));
    SpawnNeutralActor(session, "home", "Guard B", new GridPoint(1, 2));

    var waitResult = resolver.Resolve(session, ActionRequest.Wait(player.Id));

    Assert(waitResult.Success, "Expected the player's wait action to succeed.");
    Assert(hostile.Position == new GridPoint(1, 1), "Expected the blocked hostile to wait instead of overlapping another actor.");
    Assert(session.MessageLog.Any(message => message.Contains("Putit waits.", StringComparison.OrdinalIgnoreCase)), "Expected the log to record the blocked hostile waiting.");
}

static void HostileInOtherZoneDoesNotAct()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "vernis", new GridPoint(1, 1));

    var waitResult = resolver.Resolve(session, ActionRequest.Wait(player.Id));

    Assert(waitResult.Success, "Expected the player's wait action to succeed.");
    Assert(hostile.Position == new GridPoint(1, 1), "Expected hostiles outside the current zone not to receive a live turn.");
}

static void FailedPlayerActionDoesNotTriggerEnemyTurn()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(1, 1));

    SpawnNeutralActor(session, "home", "Traveler", new GridPoint(2, 1));

    var blockedMove = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));

    Assert(!blockedMove.Success, "Expected moving into a neutral actor to fail.");
    Assert(hostile.Position == new GridPoint(1, 1), "Expected a failed player action not to trigger the enemy phase.");
}

static void DefeatedPlayerCannotAct()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    player.HitPoints = 1;

    SpawnCustomHostileActor(session, "home", "Killer Bee", new GridPoint(2, 1), attackPower: 99, defense: 0);

    var firstWait = resolver.Resolve(session, ActionRequest.Wait(player.Id));
    var secondWait = resolver.Resolve(session, ActionRequest.Wait(player.Id));

    Assert(firstWait.Success, "Expected the initial wait action to resolve before the hostile counterattack.");
    Assert(!player.IsAlive, "Expected the player to be marked defeated after lethal enemy damage.");
    Assert(!secondWait.Success, "Expected a defeated player to be unable to act.");
    Assert(session.MessageLog.Any(message => message.Contains("cannot act because they are defeated", StringComparison.OrdinalIgnoreCase)), "Expected the log to explain why the defeated player cannot act.");
}

static void MoveIntoHostileTriggersMeleeAttack()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(2, 1));

    var attackResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));

    Assert(attackResult.Success, "Expected moving into a hostile target to resolve as a successful melee attack.");
    Assert(player.Position == new GridPoint(2, 2), "Expected the player to stay in place when performing a bump attack.");
    Assert(hostile.HitPoints == 1, "Expected the hostile target to lose HP based on the simple melee formula.");
    Assert(player.HitPoints == 19, "Expected the surviving hostile to counterattack during the enemy phase.");
    Assert(session.MessageLog.Any(message => message.Contains("Putit has 1/5 HP remaining", StringComparison.OrdinalIgnoreCase)), "Expected the combat log to mention the defender's remaining HP after the player's attack.");
}

static void KillingHostileSpawnsFixedDrop()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = SpawnDefinedActor(session, "putit", "home", new GridPoint(2, 1));
    hostile.HitPoints = 1;

    var killResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));
    Assert(killResult.Success, "Expected the killing blow to resolve successfully.");
    Assert(!hostile.IsAlive, "Expected the hostile actor to be marked dead after lethal damage.");

    var droppedItem = session.World.GetItemsInZone("home").FirstOrDefault(item =>
        item.DefinitionId == "putit_slime"
        && item.Position == new GridPoint(2, 1));

    Assert(droppedItem is not null, "Expected the defeated hostile to create its fixed drop.");

    var moveToLoot = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));
    Assert(moveToLoot.Success, "Expected the player to move onto the defeated enemy tile after the kill.");
    Assert(player.Position == new GridPoint(2, 1), "Expected the player to stand on the drop tile before picking it up.");

    var pickupResult = resolver.Resolve(session, ActionRequest.PickUp(player.Id, droppedItem!.Id));
    Assert(pickupResult.Success, "Expected the spawned loot to be pickable.");
    Assert(player.InventoryItemIds.Contains(droppedItem.Id), "Expected the picked-up drop to enter the inventory.");
}

static void MoveIntoNonHostileIsBlocked()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var neutral = new ActorState(
        EntityId.New(),
        "traveler",
        "Traveler",
        "N",
        "home",
        new GridPoint(2, 1),
        10,
        10,
        100,
        Faction.Neutral,
        1,
        0);
    session.World.Actors[neutral.Id] = neutral;

    var moveResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));

    Assert(!moveResult.Success, "Expected neutral actors to block movement rather than trigger combat.");
    Assert(player.Position == new GridPoint(2, 2), "Expected the player position to remain unchanged when blocked by a neutral actor.");
    Assert(neutral.HitPoints == neutral.MaxHitPoints, "Expected non-hostile actors not to take damage from movement.");
}

static void MoveAndWallResolution()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    var moveResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));
    Assert(moveResult.Success, "Expected the player to move onto an open floor tile.");
    Assert(player.Position == new GridPoint(2, 1), "Expected the player to move north one tile.");

    var wallResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));
    Assert(!wallResult.Success, "Expected the player to be blocked by the outer wall.");
    Assert(player.Position == new GridPoint(2, 1), "Expected the player position to remain unchanged after hitting a wall.");
}

static void PickUpAndDrop()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);

    var pickupResult = resolver.Resolve(session, ActionRequest.PickUp(player.Id));
    Assert(pickupResult.Success, "Expected the sword on the home tile to be picked up.");
    Assert(player.InventoryItemIds.Count == 2, "Expected both ration and sword in the inventory after pickup.");

    var dropResult = resolver.Resolve(session, ActionRequest.Drop(player.Id));
    Assert(dropResult.Success, "Expected the inventory item to be dropped.");
    Assert(player.InventoryItemIds.Count == 1, "Expected one item to remain after dropping.");
}

static void TravelUsesOpenTileWhenEntryIsOccupied()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var vernis = session.World.GetZone("vernis");
    var blocker = new ActorState(EntityId.New(), "blocker", "Blocker", "B", "vernis", vernis.EntryPoint, 10, 10, 100, Faction.Neutral, 1, 0);
    session.World.Actors[blocker.Id] = blocker;

    var travelResult = resolver.Resolve(session, ActionRequest.Travel(session.PlayerId, "vernis"));
    var player = session.World.GetPlayer(session.PlayerId);

    Assert(travelResult.Success, "Expected travel to succeed even when the target entry tile is occupied.");
    Assert(player.Position != vernis.EntryPoint, "Expected travel to pick an alternate open tile when the entry tile is blocked.");
    Assert(player.Position != blocker.Position, "Expected the player not to overlap the blocking actor after travel.");
}

static void TravelAndSaveRoundTrip()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var travelResult = resolver.Resolve(session, ActionRequest.Travel(session.PlayerId, "vernis"));
    Assert(travelResult.Success, "Expected travel to Vernis to succeed.");
    Assert(session.World.CurrentZoneId == "vernis", "Expected current zone to change after travel.");
    Assert(session.World.GetSuggestedTravelZoneId() == "puppy_cave", "Expected suggested travel to move forward along the loop.");

    var saveLoad = new SaveLoadService();
    var snapshot = saveLoad.CreateSnapshot(session);
    var restored = saveLoad.RestoreSnapshot(saveLoad.Deserialize(saveLoad.Serialize(snapshot)), BuildCatalog());

    Assert(restored.World.CurrentZoneId == "vernis", "Expected the restored session to keep the current zone.");
    Assert(restored.World.LastVisitedZoneId == "home", "Expected the restored session to keep the previous zone.");
    Assert(restored.World.GetPlayer(restored.PlayerId).InventoryItemIds.Count == 1, "Expected inventory to survive save/load.");
}

static void ZoneAssemblerRejectsWallEntryPoint()
{
    var invalidZone = new ZoneDefinition(
        "broken_zone",
        "Broken Zone",
        ZoneKind.Dungeon,
        new[]
        {
            "#####",
            "#...#",
            "#####"
        },
        new GridPoint(0, 0),
        Array.Empty<string>());

    AssertThrows<InvalidOperationException>(
        () => new ZoneAssembler().Assemble(invalidZone),
        "Expected zone assembly to reject entry points placed on wall tiles.");
}

static GameSession BuildSession()
{
    var factory = new SessionFactory(
        new FixedClockService(),
        new DefaultRngService(1337),
        new ZoneAssembler(),
        new QuestBoardService());

    var bootstrap = new GameBootstrap(factory);
    return bootstrap.CreateDefaultSession(BuildCatalog());
}

static ContentCatalog BuildCatalog()
{
    var actors = new Dictionary<string, ActorDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["player"] = new ActorDefinition("player", "Adventurer", "@", 20, 100, Faction.Player, 4, 1, "", IsPlayerTemplate: true),
        ["putit"] = new ActorDefinition("putit", "Putit", "p", 5, 80, Faction.Hostile, 2, 0, "putit_slime", KillExperienceReward: 25)
    };

    var items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["ration"] = new ItemDefinition("ration", "Ration", "!"),
        ["short_sword"] = new ItemDefinition("short_sword", "Short Sword", "/"),
        ["putit_slime"] = new ItemDefinition("putit_slime", "Putit Slime", "%")
    };

    var zones = new Dictionary<string, ZoneDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["home"] = new ZoneDefinition(
            "home",
            "Home",
            ZoneKind.Home,
            new[]
            {
                "#####",
                "#...#",
                "#...#",
                "#...#",
                "#####"
            },
            new GridPoint(2, 2),
            new[] { "vernis" }),
        ["vernis"] = new TownDefinition(
            "vernis",
            "Vernis",
            new[]
            {
                "#######",
                "#.....#",
                "#.....#",
                "#.....#",
                "#######"
            },
            new GridPoint(3, 2),
            new[] { "home", "puppy_cave" }),
        ["puppy_cave"] = new DungeonDefinition(
            "puppy_cave",
            "Puppy Cave",
            new[]
            {
                "######",
                "#....#",
                "#.##.#",
                "#....#",
                "######"
            },
            new GridPoint(1, 1),
            new[] { "vernis" },
            "cave_low_level")
    };

    var quests = new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["vermin_cleanup"] = new QuestDefinition(
            "vermin_cleanup",
            "Vermin Cleanup",
            "Clear the first cave and come back alive.",
            new[]
            {
                new QuestObjectiveDefinition("primary", QuestObjectiveKind.ClearHostilesInZone, "puppy_cave", RequiredAmount: 1)
            },
            new QuestTurnInDefinition(QuestTurnInKind.ManualAtZone, "vernis"),
            new[]
            {
                new QuestRewardDefinition(QuestRewardKind.Gold, 500),
                new QuestRewardDefinition(QuestRewardKind.Experience, 90)
            },
            Array.Empty<QuestFailureDefinition>())
    };

    var spawnTables = new Dictionary<string, SpawnTableDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["cave_low_level"] = new SpawnTableDefinition("cave_low_level", new[] { "putit" })
    };
    return new ContentCatalog(actors, items, zones, quests, spawnTables);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static ActorState SpawnDefinedActor(GameSession session, string definitionId, string zoneId, GridPoint position)
{
    var definition = session.Content.Actors[definitionId];
    var actor = new ActorState(
        EntityId.New(),
        definition.Id,
        definition.DisplayName,
        definition.Glyph,
        zoneId,
        position,
        definition.MaxHitPoints,
        definition.MaxHitPoints,
        definition.ActionPointsPerTurn,
        definition.Faction,
        definition.AttackPower,
        definition.Defense);

    session.World.Actors[actor.Id] = actor;
    return actor;
}

static ActorState SpawnNeutralActor(GameSession session, string zoneId, string displayName, GridPoint position)
{
    var actor = new ActorState(
        EntityId.New(),
        "neutral_actor",
        displayName,
        "N",
        zoneId,
        position,
        10,
        10,
        100,
        Faction.Neutral,
        1,
        0);

    session.World.Actors[actor.Id] = actor;
    return actor;
}

static ActorState SpawnCustomHostileActor(GameSession session, string zoneId, string displayName, GridPoint position, int attackPower, int defense)
{
    var actor = new ActorState(
        EntityId.New(),
        "custom_hostile",
        displayName,
        "h",
        zoneId,
        position,
        10,
        10,
        100,
        Faction.Hostile,
        attackPower,
        defense);

    session.World.Actors[actor.Id] = actor;
    return actor;
}

static GameSession BuildReadyToTurnInQuestSession()
{
    var session = BuildSession();
    var resolver = new ActionResolver();
    var player = session.World.GetPlayer(session.PlayerId);
    var hostile = session.World.GetActorsInZone("puppy_cave").Single(actor => !actor.IsPlayer);

    hostile.Position = new GridPoint(2, 1);
    hostile.HitPoints = 1;

    resolver.Resolve(session, ActionRequest.Travel(player.Id, "vernis"));
    resolver.Resolve(session, ActionRequest.Travel(player.Id, "puppy_cave"));
    player.Position = new GridPoint(2, 2);

    var attackResult = resolver.Resolve(session, ActionRequest.Move(player.Id, MoveDirection.Up));
    Assert(attackResult.Success, "Expected the setup attack to succeed.");
    Assert(session.World.Quests["vermin_cleanup"].Status == QuestStatus.ReadyToTurnIn, "Expected the setup flow to produce a ready-to-turn-in quest.");

    var returnResult = resolver.Resolve(session, ActionRequest.Travel(player.Id, "vernis"));
    Assert(returnResult.Success, "Expected returning to Vernis during setup to succeed.");

    return session;
}

static string BuildLegacyV1Json(SaveLoadService saveLoad, GameSession session, bool removeQuestRewardExperience)
{
    var snapshot = saveLoad.CreateSnapshot(session);
    snapshot.Version = 1;

    var root = JsonNode.Parse(saveLoad.Serialize(snapshot))!.AsObject();
    var actors = root["World"]!["Actors"]!.AsArray();
    foreach (var actorNode in actors)
    {
        actorNode!.AsObject().Remove("Level");
        actorNode!.AsObject().Remove("CurrentExp");
    }

    if (removeQuestRewardExperience)
    {
        var quests = root["World"]!["Quests"]!.AsArray();
        foreach (var questNode in quests)
        {
            questNode!.AsObject().Remove("RewardExperience");
        }
    }

    return root.ToJsonString(new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true
    });
}
