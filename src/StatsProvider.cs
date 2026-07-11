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

    public static void OnRoundEnd()
    {
        _roundNumber++;
        Server.NextWorldUpdate(() =>
        {
            try
            {
                _cachedStatsJson = BuildStatsJson();
                _cachedMatchStatusJson = BuildMatchStatusJson();
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
            sb.Append("\"steamId\":").Append(p.SteamID);
            sb.Append(",\"name\":\"").Append(EscapeJson(p.PlayerName)).Append('"');
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
                sb.Append(",\"flashAssists\":").Append(stats.FlashAssists);
                sb.Append(",\"shotsFired\":").Append(stats.ShotsFired);
                sb.Append(",\"shotsOnTarget\":").Append(stats.ShotsOnTarget);
                sb.Append(",\"totalDamage\":").Append(stats.TotalDamage);
                sb.Append(",\"entryCount\":").Append(stats.Enemy2K + stats.Enemy3K + stats.Enemy4K + stats.Enemy5K);
                sb.Append(",\"oneVoneCount\":").Append(stats.OneVoneCount);
                sb.Append(",\"oneVoneWins\":").Append(stats.OneVoneWon);
                sb.Append(",\"entryKills\":").Append(stats.Entrancekills);
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

    private static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
