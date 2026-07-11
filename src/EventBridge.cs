using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchUp;

/*
 * EventBridge is used to centralize all event logging to the console as JSON.
 * This is used by the Discord bot to bridge game events to Discord.
 */
public static class EventBridge
{
    private static string? _lastSentStatus = null;

    private static void Print(string tag, string json)
    {
        if (!MatchConfig.EventBridgeEnabled) return;
        Console.WriteLine($"\n[MATCHUP_{tag.ToUpper()}] {json}\n");
    }

    public static void OnChat(EventPlayerChat @event)
    {
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid) return;

        var side = player.TeamNum switch
        {
            (byte)CsTeam.Terrorist => "T",
            (byte)CsTeam.CounterTerrorist => "CT",
            (byte)CsTeam.Spectator => "spectator",
            _ => "other"
        };

        Print("CHAT", $"{{\"player\":{{\"name\":\"{EscapeJson(player.PlayerName)}\",\"steamId64\":{player.SteamID},\"side\":\"{side}\"}},\"message\":\"{EscapeJson(@event.Text)}\",\"isTeamChat\":{@event.Teamonly.ToString().ToLower()}}}");
    }

    public static void OnStateChange(GameState oldState, GameState newState)
    {
        Print("STATE_CHANGE", $"{{\"oldState\":\"{oldState.ToString().ToLower()}\",\"newState\":\"{newState.ToString().ToLower()}\"}}");

        // Only send webhooks in pelipaja mode
        if (PelipajaConfig.Mode != "pelipaja") return;

        var status = newState switch
        {
            GameState.PelipajaWaiting => "configuring",
            GameState.ReadyUp => PelipajaConfig.Mode == "pelipaja" ? "ready" : null,
            GameState.Live => "live",
            GameState.Loading when oldState == GameState.Live => "finished",
            _ => null
        };

        // Only send webhook if status changed (prevents duplicate requests)
        if (status != null && status != _lastSentStatus)
        {
            _lastSentStatus = status;
            if (status == "finished")
                WebhookClient.PostStatus(status, StatsProvider.GetStatsJson());
            else
                WebhookClient.PostStatus(status);
        }
    }

    public static void OnPlayerConnect(EventPlayerConnectFull @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return;

        Print("PLAYER_CONNECT", $"{{\"name\":\"{EscapeJson(player.PlayerName)}\",\"steamId64\":{player.SteamID}}}");
    }

    public static void OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return;

        Print("PLAYER_DISCONNECT", $"{{\"name\":\"{EscapeJson(player.PlayerName)}\",\"steamId64\":{player.SteamID}}}");
    }

    public static void OnPlayerTeam(EventPlayerTeam @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return;

        var side = @event.Team switch
        {
            (byte)CsTeam.Terrorist => "T",
            (byte)CsTeam.CounterTerrorist => "CT",
            (byte)CsTeam.Spectator => "spectator",
            _ => "other"
        };

        Print("PLAYER_TEAM", $"{{\"name\":\"{EscapeJson(player.PlayerName)}\",\"steamId64\":{player.SteamID},\"side\":\"{side}\"}}");
    }

    public static void OnReset()
    {
        Print("RESET", "{}");
    }

    public static void OnRoundEnd(EventRoundEnd @event)
    {
        var winner = @event.Winner switch
        {
            (byte)CsTeam.Terrorist => "T",
            (byte)CsTeam.CounterTerrorist => "CT",
            _ => "none"
        };

        Print("ROUND_END", $"{{\"winner\":\"{winner}\",\"reason\":{@event.Reason}}}");
    }

    public static void OnMatchEnd(EventCsWinPanelMatch @event, int ctScore, int tScore)
    {
        var winner = ctScore > tScore ? "CT" : (tScore > ctScore ? "T" : "draw");
        Print("MATCH_END", $"{{\"ctScore\":{ctScore},\"tScore\":{tScore},\"winner\":\"{winner}\"}}");
    }

    public static void OnPause(string side)
    {
        Print("PAUSE", $"{{\"side\":\"{side}\"}}");
    }

    public static void OnUnpause(string side)
    {
        Print("UNPAUSE", $"{{\"side\":\"{side}\"}}");
    }

    public static void OnBackup(string adminName, int round)
    {
        Print("BACKUP_RESTORE", $"{{\"admin\":\"{EscapeJson(adminName)}\",\"round\":{round}}}");
    }

    private static string EscapeJson(string s) => Utils.EscapeJson(s);
}
