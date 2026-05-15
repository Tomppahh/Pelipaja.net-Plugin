using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchUp;

public class TeamInfo {
    public required string Name { get; set; }
    public List<string> Players { get; set; } = [];
    
}


public static class PelipajaConfig {
    // these are set once the container starts then dont change
    public static string? WebhookUrl { get; private set; }
    public static string? ApiSecret { get; private set; }
    
    // these are set when HTTP config arrives from Next.js
    public static string Mode { get; private set; } = "manual"; // this is a switch between matchup old config and new pelipaja.net config
    public static string? MatchId { get; private set; }
    public static TeamInfo? Team1 { get; private set; }
    public static TeamInfo? Team2 { get; private set; } 
    public static string? OwnerSteamId { get; private set; }

    public static void Load()
    {
        WebhookUrl = Environment.GetEnvironmentVariable("MATCHUP_WEBHOOK_URL");
        ApiSecret = Environment.GetEnvironmentVariable("MATCHUP_API_SECRET");
        MatchId = Environment.GetEnvironmentVariable("MATCHUP_MATCH_ID");

        Console.WriteLine($"[Pelipaja] WebhookUrl: {WebhookUrl}");
        Console.WriteLine($"[Pelipaja] ApiSecret set: {ApiSecret!=null}"); // sends a boolean true false
        Console.WriteLine($"[Pelipaja] MatchId: {MatchId}");

        WebhookClient.PostStatus("configuring"); // tell Nextjs that server is online to receive HTTP config
    }

    public static void SetMatchConfig(string mode, string matchId, string? ownerSteamId, TeamInfo team1, TeamInfo team2)
    {
        Mode = mode;
        MatchId = matchId;
        OwnerSteamId = ownerSteamId;
        Team1 = NormalizeTeam(team1);
        Team2 = NormalizeTeam(team2);

        Console.WriteLine($"[Pelipaja] Config received - Mode: {Mode}, MatchId: {MatchId}");
        Console.WriteLine($"[Pelipaja] Team1: {Team1.Name}, Team2: {Team2.Name}");
        Console.WriteLine($"[Pelipaja] Team1 Players: {string.Join(", ", Team1.Players)}");
        Console.WriteLine($"[Pelipaja] Team2 Players: {string.Join(", ", Team2.Players)}");
    }

    public static void AssignTeamIfConfigured(CCSPlayerController player)
    {
        if (Team1 == null || Team2 == null) return;

        var steamId = player.SteamID.ToString();
        if (Team1.Players.Contains(steamId))
        {
            player.ChangeTeam(CsTeam.Terrorist);
            player.PrintToChat($" {ChatColors.Green}You have been assigned to {Team1.Name}");
            return;
        }

        if (Team2.Players.Contains(steamId))
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            player.PrintToChat($" {ChatColors.Green}You have been assigned to {Team2.Name}");
        }
    }

    public static void AssignConnectedPlayers()
    {
        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        foreach (var player in playerEntities)
        {
            if (!player.IsValid) continue;
            AssignTeamIfConfigured(player);
        }
    }

    private static TeamInfo NormalizeTeam(TeamInfo team)
    {
        team.Name = team.Name.Trim();
        team.Players = team.Players
            .Where(playerId => !string.IsNullOrWhiteSpace(playerId))
            .Select(playerId => playerId.Trim())
            .ToList();

        return team;
    }
    
}