using ElonaClone.Content;
using ElonaClone.Game.Application;
using ElonaClone.Game.Persistence;
using ElonaClone.Game.Simulation;
using ElonaClone.Presentation;
using Godot;

public partial class Node2d : Node2D
{
    private readonly InputRouter _inputRouter = new();
    private readonly MessageLogPresenter _messageLogPresenter = new();
    private readonly ActionResolver _actionResolver = new();

    private GameSession? _session;
    private RichTextLabel? _status;

    public override void _Ready()
    {
        _status = GetNode<RichTextLabel>("Hud/Status");

        var contentRegistry = new ContentRegistry();
        var contentCatalog = contentRegistry.LoadBuiltInCatalog();
        var bootstrap = new GameBootstrap(
            new SessionFactory(
                new FixedClockService(),
                new DefaultRngService(42),
                new ZoneAssembler(),
                new QuestBoardService()));

        var session = bootstrap.CreateDefaultSession(contentCatalog);
        var saveLoad = new SaveLoadService();
        _session = saveLoad.RestoreSnapshot(saveLoad.CreateSnapshot(session), contentCatalog);
        _session.AddMessage("Save/load round-trip verified during startup.");

        GD.Print("ElonaClone phase-0/1 combat runtime ready.");
        RefreshStatus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_session is null)
        {
            return;
        }

        var request = _inputRouter.TryMap(@event, _session);
        if (request is null)
        {
            return;
        }

        var result = _actionResolver.Resolve(_session, request);
        foreach (var effect in result.Effects)
        {
            GD.Print(effect.Description);
        }

        RefreshStatus();
        GetViewport().SetInputAsHandled();
    }

    private void RefreshStatus()
    {
        if (_status is null || _session is null)
        {
            return;
        }

        _status.Text = _messageLogPresenter.Format(_session);
    }
}
