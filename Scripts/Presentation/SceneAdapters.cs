using ElonaClone.Game;
using ElonaClone.Game.Simulation;
using Godot;

namespace ElonaClone.Presentation;

public partial class WorldMapScene : Node2D
{
}

public partial class TownScene : Node2D
{
}

public partial class DungeonScene : Node2D
{
}

public partial class HudRoot : CanvasLayer
{
}

public partial class SelectionCursor : Node2D
{
}

public sealed class InputRouter
{
    public ActionRequest? TryMap(InputEvent @event, GameSession session)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return null;
        }

        return keyEvent.Keycode switch
        {
            Key.Up => ActionRequest.Move(session.PlayerId, MoveDirection.Up),
            Key.Down => ActionRequest.Move(session.PlayerId, MoveDirection.Down),
            Key.Left => ActionRequest.Move(session.PlayerId, MoveDirection.Left),
            Key.Right => ActionRequest.Move(session.PlayerId, MoveDirection.Right),
            Key.Space => ActionRequest.Wait(session.PlayerId),
            Key.G => ActionRequest.PickUp(session.PlayerId),
            Key.D => ActionRequest.Drop(session.PlayerId),
            Key.C => ActionRequest.TurnInQuest(session.PlayerId),
            Key.T => BuildTravelRequest(session),
            _ => null
        };
    }

    private static ActionRequest? BuildTravelRequest(GameSession session)
    {
        var targetZoneId = session.World.GetSuggestedTravelZoneId();
        return targetZoneId is null
            ? null
            : ActionRequest.Travel(session.PlayerId, targetZoneId);
    }
}

public sealed class MessageLogPresenter
{
    public string Format(GameSession session)
    {
        var world = session.World;
        var zone = world.GetCurrentZone();
        var player = world.GetPlayer(session.PlayerId);

        var lines = new List<string>
        {
            "ELONA CLONE RUNTIME SKELETON",
            $"Zone: {zone.DisplayName} [{zone.Kind}]   Day {world.Day}   Time {world.MinuteOfDay / 60:00}:{world.MinuteOfDay % 60:00}",
            $"Player Lv {player.Level}   EXP {player.CurrentExp}/{player.ExpToNextLevel}   HP {player.HitPoints}/{player.MaxHitPoints}   ATK {player.AttackPower}   DEF {player.Defense}   Gold {player.Gold}{(player.IsAlive ? string.Empty : "   [DEFEATED]")}",
            "Controls: Arrow keys move/attack | G pick up | D drop | C claim quest | Space wait | T travel",
            string.Empty
        };

        lines.AddRange(RenderZone(zone, world));
        lines.Add(string.Empty);

        var travelTargets = zone.ConnectedZoneIds.Count == 0 ? "-" : string.Join(", ", zone.ConnectedZoneIds);
        lines.Add($"Travel targets: {travelTargets}");
        lines.Add($"Suggested travel: {world.GetSuggestedTravelZoneId() ?? "-"}");

        var inventory = player.InventoryItemIds
            .Select(itemId => world.Items.TryGetValue(itemId, out var item) ? item.DisplayName : itemId.ToString())
            .ToArray();
        lines.Add($"Inventory: {(inventory.Length == 0 ? "(empty)" : string.Join(", ", inventory))}");

        var quests = world.Quests.Values.Select(DescribeQuest).ToArray();
        lines.Add($"Quest board: {(quests.Length == 0 ? "(empty)" : string.Join(" | ", quests))}");

        lines.Add("Recent log:");
        foreach (var message in session.GetRecentMessages())
        {
            lines.Add($"- {message}");
        }

        return "[code]" + string.Join("\n", lines) + "[/code]";
    }

    private static IEnumerable<string> RenderZone(ZoneState zone, WorldState world)
    {
        var rows = new List<char[]>();
        for (var y = 0; y < zone.Height; y++)
        {
            var row = new char[zone.Width];
            for (var x = 0; x < zone.Width; x++)
            {
                row[x] = zone.GetTile(new GridPoint(x, y)) == TileType.Wall ? '#' : '.';
            }

            rows.Add(row);
        }

        foreach (var item in world.GetItemsInZone(zone.Id))
        {
            if (item.Position is not null)
            {
                rows[item.Position.Value.Y][item.Position.Value.X] = GlyphToChar(item.Glyph, '!');
            }
        }

        foreach (var actor in world.GetActorsInZone(zone.Id))
        {
            rows[actor.Position.Y][actor.Position.X] = GlyphToChar(actor.Glyph, '@');
        }

        return rows.Select(row => new string(row));
    }

    private static char GlyphToChar(string glyph, char fallback) => string.IsNullOrWhiteSpace(glyph) ? fallback : glyph[0];

    private static string DescribeQuest(QuestState quest)
    {
        var statusText = quest.Status switch
        {
            QuestStatus.Accepted => $"Accepted -> {quest.TargetZoneId}",
            QuestStatus.ReadyToTurnIn => $"Ready -> {quest.TurnInZoneId}",
            QuestStatus.Completed => "Completed",
            QuestStatus.Failed => "Failed",
            _ => quest.Status.ToString()
        };

        var rewards = new List<string>();
        if (quest.RewardGold > 0)
        {
            rewards.Add($"{quest.RewardGold}gp");
        }

        if (quest.RewardExperience > 0)
        {
            rewards.Add($"{quest.RewardExperience}xp");
        }

        var rewardText = rewards.Count == 0 ? "-" : string.Join(" + ", rewards);
        return $"{quest.Title} [{statusText}] ({rewardText})";
    }
}
