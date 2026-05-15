using CounterStrikeSharp.API.Core;

namespace MatchUp.states;

public class PelipajaWaitingState : BaseState
{
    public override void Enter(GameState oldState)
    {
        Console.WriteLine("[Pelipaja] Waiting for config from Next.js...");
    }

    public override void Leave() { }

    public override void OnPlayerConnect(EventPlayerConnectFull @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return;

        PelipajaConfig.AssignTeamIfConfigured(player);
    }
}