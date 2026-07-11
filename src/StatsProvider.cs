using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchUp;

public static class StatsProvider
{
    private static string _cachedStatsJson = "{}";
    private static string _cachedMatchStatusJson = "{}";
    private static int _roundNumber;

    // Entry kill tracking (minimal overhead: bool + dictionary lookup per kill)
    private static bool _entryKillRecorded;
    private static readonly Dictionary<ulong, int> _entryKills = new();
    private static readonly Dictionary<ulong, int> _entryDeaths = new();

    public static void OnRoundStart()
    {
        _entryKillRecorded = false;
    }

    public static void OnPlayerDeath(CCSPlayerController? attacker, CCSPlayerController? victim)
    {
        if (_entryKillRecorded) return;
        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid) return;
        if (attacker.SteamID == 0 || victim.SteamID == 0) return;
        if (attacker.TeamNum == victim.TeamNum) return; // teamkill, not an entry

        _entryKillRecorded = true;

        if (!_entryKills.ContainsKey(attacker.SteamID))
            _entryKills[attacker.SteamID] = 0;
        _entryKills[attacker.SteamID]++;

        if (!_entryDeaths.ContainsKey(victim.SteamID))
            _entryDeaths[victim.SteamID] = 0;
        _entryDeaths[victim.SteamID]++;
    }

    public static void OnRoundEnd()
    {
        _roundNumber++;
        _entryKillRecorded = false;
        Server.NextWorldUpdate(() =>
        {
            try
            {
                _cachedStatsJson = BuildStatsJson();
                _cachedMatchStatusJson = BuildMatchStatusJson();
                // Push the updated stats to the site on every round end so the
                // match page updates instantly. Fired from a scheduled world-update
                // tick (not the event handler) and sent on a background HTTP task,
                // so this has no impact on the game thread or player experience.
                WebhookClient.PostStatus("round_end", _cachedStatsJson);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Pelipaja] StatsProvider error: {e.Message}");
            }
        });
    }

    public static void OnMatchStart()
    {
        _roundNumber = 0;
        _cachedStatsJson = "{}";
        _cachedMatchStatusJson = "{}";
    }

    public static string GetStatsJson() => _cachedStatsJson;
    public static string GetMatchStatusJson() => _cachedMatchStatusJson;

    private static string BuildStatsJson()
    {
        var sb = new StringBuilder();
        sb.Append('{');

        // Map info
        sb.Append("\"map\":\"").Append(Server.MapName).Append("\",");

        // Team scores
        var ctScore = 0;
        var tScore = 0;
        foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
        {
            if (team.TeamNum == (byte)CsTeam.CounterTerrorist) ctScore = team.Score;
            else if (team.TeamNum == (byte)CsTeam.Terrorist) tScore = team.Score;
        }
        sb.Append("\"score\":{\"ct\":").Append(ctScore).Append(",\"t\":").Append(tScore).Append("},");
        sb.Append("\"round\":").Append(_roundNumber).Append(",");

        // Players
        sb.Append("\"players\":[");
        var first = true;
        foreach (var p in Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller"))
        {
            if (!p.IsValid || p.SteamID == 0) continue;

            if (!first) sb.Append(',');
            first = false;

            var side = p.TeamNum switch
            {
                (byte)CsTeam.Terrorist => "T",
                (byte)CsTeam.CounterTerrorist => "CT",
                _ => "other"
            };

            var stats = p.ActionTrackingServices?.MatchStats;

            sb.Append('{');
            sb.Append("\"steamId\":").Append(p.SteamID == 0 ? -(int)p.Index - 1 : (long)p.SteamID);
            sb.Append(",\"name\":\"").Append(EscapeJson(p.PlayerName)).Append('"');
            sb.Append(",\"isBot\":").Append(p.IsBot.ToString().ToLower());
            sb.Append(",\"team\":\"").Append(side).Append('"');
            sb.Append(",\"score\":").Append(p.Score);
            sb.Append(",\"mvs\":").Append(p.MVPs);
            sb.Append(",\"ping\":").Append(p.Ping);

            if (stats != null)
            {
                sb.Append(",\"kills\":").Append(stats.Kills);
                sb.Append(",\"deaths\":").Append(stats.Deaths);
                sb.Append(",\"assists\":").Append(stats.Assists);
                sb.Append(",\"headshotKills\":").Append(stats.HeadShotKills);
                sb.Append(",\"utilityDamage\":").Append(stats.UtilityDamage);
                sb.Append(",\"flashAssists\":").Append(stats.Flash_Successes);
                sb.Append(",\"shotsFired\":").Append(stats.ShotsFiredTotal);
                sb.Append(",\"shotsOnTarget\":").Append(stats.ShotsOnTargetTotal);
                sb.Append(",\"totalDamage\":").Append(stats.Damage);
                sb.Append(",\"entryCount\":").Append(stats.EntryCount);
                sb.Append(",\"entryKills\":").Append(_entryKills.TryGetValue(p.SteamID, out var ek) ? ek : 0);
                sb.Append(",\"entryDeaths\":").Append(_entryDeaths.TryGetValue(p.SteamID, out var ed) ? ed : 0);
                sb.Append(",\"oneVoneCount\":").Append(stats.I1v1Count);
                sb.Append(",\"oneVoneWins\":").Append(stats.I1v1Wins);
            }

            sb.Append('}');
        }
        sb.Append("]}");

        return sb.ToString();
    }

    private static string BuildMatchStatusJson()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"state\":\"").Append(StateMachine.GetCurrentGameState().ToString().ToLower()).Append("\",");
        sb.Append("\"map\":{\"name\":\"").Append(Server.MapName).Append("\",\"workshopId\":\"").Append(MatchConfig.Map.WorkshopId ?? "").Append("\"},");

        var ctScore = 0;
        var tScore = 0;
        foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
        {
            if (team.TeamNum == (byte)CsTeam.CounterTerrorist) ctScore = team.Score;
            else if (team.TeamNum == (byte)CsTeam.Terrorist) tScore = team.Score;
        }
        sb.Append("\"score\":{\"ct\":").Append(ctScore).Append(",\"t\":").Append(tScore).Append("},");

        sb.Append("\"config\":{\"playersPerTeam\":").Append(MatchConfig.PlayersPerTeam);
        sb.Append(",\"maxPlayers\":").Append(MatchConfig.PlayersPerTeam * 2);
        sb.Append(",\"knifeRound\":").Append(MatchConfig.KnifeRound.ToString().ToLower()).Append("},");

        sb.Append("\"players\":[");
        var first = true;
        foreach (var p in Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller"))
        {
            if (!p.IsValid || p.SteamID == 0) continue;

            if (!first) sb.Append(',');
            first = false;

            var side = p.TeamNum switch
            {
                (byte)CsTeam.Terrorist => "T",
                (byte)CsTeam.CounterTerrorist => "CT",
                (byte)CsTeam.Spectator => "spectator",
                _ => "other"
            };

            sb.Append('{');
            sb.Append("\"name\":\"").Append(EscapeJson(p.PlayerName)).Append('"');
            sb.Append(",\"steamId64\":").Append(p.SteamID);
            sb.Append(",\"isBot\":").Append(p.IsBot.ToString().ToLower());
            sb.Append(",\"side\":\"").Append(side).Append('"');
            sb.Append('}');
        }
        sb.Append("]}");

        return sb.ToString();
    }

    private static string EscapeJson(string s) => Utils.EscapeJson(s);
}
