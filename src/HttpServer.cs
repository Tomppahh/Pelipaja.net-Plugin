using System.Net;
using System.Text.Json;
using CounterStrikeSharp.API;

namespace MatchUp;

public static class HttpServer
{
    private static HttpListener? _listener;

    public static void Start()
    {
        try
        {
            var port = Environment.GetEnvironmentVariable("MATCHUP_API_PORT") ?? "27090";

            _listener =  new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");
            _listener.Start();
            Console.WriteLine($"[Pelipaja] HTTP server listening on port {port}");
            Task.Run(() => Listen());
        }
        catch(Exception e)
        {
            Console.WriteLine($"Error during HTTPServer Start(): {e.Message}");
        }
    }

    public static void Stop()
    {
        _listener?.Stop();
        Console.WriteLine("[Pelipaja] HTTP server stopped.");
    }

    private static async Task Listen()
    {
        while (_listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequest(context);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in Task Listen(): {e.Message}");
                break;
            }
        }
    }

    private static async Task HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        var secret = (req.Headers["Authorization"] ?? "").Replace("Bearer ", "");
        if (string.IsNullOrEmpty(PelipajaConfig.ApiSecret) || secret != PelipajaConfig.ApiSecret)
        {
            res.StatusCode = 401;
            res.Close();
            return;
        }

        if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/config")
        {
            using var reader = new StreamReader(req.InputStream);
            var body = await reader.ReadToEndAsync();

            var payload = JsonSerializer.Deserialize<MatchConfigPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload != null)
            {
                Console.WriteLine($"[Pelipaja] Config received for match {payload.MatchId}");

                PelipajaConfig.SetMatchConfig(
                    payload.Mode,
                    payload.MatchId ?? "",
                    payload.OwnerSteamId,
                    payload.Team1 ?? new TeamInfo { Name = "Team 1" },
                    payload.Team2 ?? new TeamInfo { Name = "Team 2" }
                );

                if (!Utils.IsValidMapName(payload.Map))
                {
                    Console.WriteLine($"[Pelipaja] Invalid map name '{payload.Map}', rejecting config");
                    res.StatusCode = 400;
                    res.Close();
                    return;
                }

                Server.NextWorldUpdate(() => {
                    MatchConfig.SetMap(payload.Map);
                    if (!string.IsNullOrEmpty(payload.WorkshopId) && Utils.IsValidWorkshopId(payload.WorkshopId))
                    {
                        MatchConfig.Map.WorkshopId = payload.WorkshopId;
                        Console.WriteLine($"[Pelipaja] Workshop map ID: {payload.WorkshopId}");
                    }
                    MatchConfig.SetTeamSize(payload.TeamSize.ToString());
                    MatchConfig.SetKnife(payload.KnifeRound.ToString());
                    MatchConfig.StartMatch();
                });
            }
            res.StatusCode = 200;
        }
        else if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/stats")
        {
            var json = StatsProvider.GetStatsJson();
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.ContentLength64 = buffer.Length;
            await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            res.StatusCode = 200;
        }
        else if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/status")
        {
            var json = StatsProvider.GetMatchStatusJson();
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.ContentLength64 = buffer.Length;
            await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            res.StatusCode = 200;
        }
        else
        {
            res.StatusCode = 404;
        }

        res.Close();
    }
}

public class MatchConfigPayload
{
    public string Mode { get; set; } = "manual";
    public string? MatchId { get; set; }
    public string? Map { get; set; }
    public string? WorkshopId { get; set; }
    public int TeamSize { get; set; } = 5;
    public bool KnifeRound { get; set; } = true;
    public TeamInfo? Team1 { get; set; }
    public TeamInfo? Team2 { get; set; }
    public string? OwnerSteamId { get; set; }
}