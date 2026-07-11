using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchUp.states;

public class PelipajaWaitingState : BaseState
{
    public override void Enter(GameState oldState)
    {
        Console.WriteLine("[Pelipaja] Waiting for config from Next.js...");

        // Workshop map loaded — config was already received and StartMatch() issued host_workshop_map.
        // We ended up here because LoadingState.OnMapStart() transitions to PelipajaWaiting for pelipaja mode.
        // Since the map is already loaded and config is set, go straight to ReadyUp.
        if (PelipajaConfig.MatchStarted)
        {
            Console.WriteLine("[Pelipaja] Match already started, skipping PelipajaWaiting");
            return;
        }
    }

    public override void Leave() { }

    public override void OnPlayerConnect(EventPlayerConnectFull @event)
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
}