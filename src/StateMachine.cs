using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MatchUp.states;

namespace MatchUp;

public enum GameState
{
    Loading,
    Setup,
    ReadyUp,
    Knife,
    Live,
    End,
    PelipajaWaiting
}

public abstract class BaseState
{
    protected readonly Dictionary<string, Action<int, string[]?>> CommandActions = new();

    public abstract void Enter(GameState oldState);
    public abstract void Leave();

    public virtual void OnMapStart() { }

    public virtual void OnPlayerTeam(EventPlayerTeam @event) { }

    public virtual void OnPlayerConnect(EventPlayerConnectFull @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return;

        var steamId = player.SteamID.ToString();

        if (PelipajaConfig.Team1?.Players.Contains(steamId) == true)
        {
            player.ChangeTeam(CsTeam.Terrorist);
            player.PrintToChat($" {ChatColors.Green}You have been assigned to {PelipajaConfig.Team1.Name}");
        }
        else if (PelipajaConfig.Team2?.Players.Contains(steamId) == true)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            player.PrintToChat($" {ChatColors.Green}You have been assigned to {PelipajaConfig.Team2.Name}");
        }
    }

    public virtual void OnMatchEnd(EventCsWinPanelMatch @event) { }

    public virtual void OnRoundEnd(EventRoundEnd @event) { }

    public virtual void OnChatCommand(int userid, string command, string[]? args = null)
    {
        if (CommandActions.TryGetValue(command, out var action))
        {
            action(userid, args);
        }
    }
}

public static class StateMachine
{
    private static GameState _currentGameState;
    private static readonly Dictionary<GameState, BaseState> GameStates = new()
    {
        { GameState.Loading, new LoadingState() },
        { GameState.Setup, new SetupState() },
        { GameState.ReadyUp, new ReadyUpState() },
        { GameState.Live, new LiveState() },
        { GameState.Knife, new KnifeState() },
        { GameState.PelipajaWaiting, new PelipajaWaitingState() },
    };

    public static void SwitchState(GameState state)
    {
        var oldState = _currentGameState;
        GameStates[_currentGameState].Leave();
        GameStates[state].Enter(_currentGameState);

        _currentGameState = state;
        EventBridge.OnStateChange(oldState, _currentGameState);
    }

    public static BaseState GetCurrentState()
    {
        return GameStates[_currentGameState];
    }

    public static GameState GetCurrentGameState()
    {
        return _currentGameState;
    }
}