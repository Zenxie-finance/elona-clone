namespace ElonaClone.Game;

public readonly record struct EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}

public readonly record struct GridPoint(int X, int Y)
{
    public static GridPoint Zero => new(0, 0);

    public static GridPoint operator +(GridPoint left, GridPoint right) => new(left.X + right.X, left.Y + right.Y);
}

public enum ZoneKind
{
    Home,
    Town,
    Dungeon
}

public enum TileType
{
    Floor,
    Wall
}

public enum Faction
{
    Player,
    Hostile,
    Neutral
}

public enum ActionKind
{
    Wait,
    Move,
    Travel,
    PickUp,
    Drop,
    TurnInQuest
}

public enum MoveDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class MoveDirectionExtensions
{
    public static GridPoint ToOffset(this MoveDirection direction) => direction switch
    {
        MoveDirection.Up => new GridPoint(0, -1),
        MoveDirection.Down => new GridPoint(0, 1),
        MoveDirection.Left => new GridPoint(-1, 0),
        MoveDirection.Right => new GridPoint(1, 0),
        _ => GridPoint.Zero
    };
}

public static class FactionExtensions
{
    public static bool IsHostileTo(this Faction self, Faction other) => (self, other) switch
    {
        (Faction.Player, Faction.Hostile) => true,
        (Faction.Hostile, Faction.Player) => true,
        _ => false
    };
}

public enum QuestStatus
{
    Accepted,
    ReadyToTurnIn,
    Completed,
    Failed
}
