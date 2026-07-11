using System.Net.Http.Headers;
using System.Text.Json;
using CounterStrikeSharp.API;

namespace MatchUp;

public static class WebhookClient
{
    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
        MaxResponseContentBufferSize = 1024
    };

    private static readonly MediaTypeHeaderValue _jsonMediaType = new("application/json");
    private static readonly Queue<string> _statusQueue = new();
    private static string? _statsPayload;
    private static bool _isProcessing = false;
    private static readonly object _queueLock = new();

    public static void PostStatus(string status, string? statsJson = null)
    {
        lock (_queueLock)
        {
            _statusQueue.Enqueue(status);
            if (statsJson != null)
                _statsPayload = statsJson;
        }
        _ = ProcessQueueAsync();
    }

    private static async Task ProcessQueueAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            while (true)
            {
                string status;
                lock (_queueLock)
                {
                    if (_statusQueue.Count == 0) break;
                    status = _statusQueue.Dequeue();
                }

                await SendStatusAsync(status);
                await Task.Delay(100);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static async Task SendStatusAsync(string status)
    {
        var webhookUrl = PelipajaConfig.WebhookUrl;
        var matchId = PelipajaConfig.MatchId;

        if (string.IsNullOrEmpty(webhookUrl) || string.IsNullOrEmpty(matchId))
        {
            Console.WriteLine("[Pelipaja] WebhookUrl or MatchId not set, skipping webhook");
            return;
        }

        try
        {
            string json;
            string? statsToSend = null;
            lock (_queueLock)
            {
                statsToSend = _statsPayload;
                _statsPayload = null;
            }

            if (statsToSend != null)
            {
                json = $"{{\"status\":\"{status}\",\"stats\":{statsToSend}}}";
            }
            else
            {
                json = JsonSerializer.Serialize(new { status });
            }

            using var content = new StringContent(json, System.Text.Encoding.UTF8, _jsonMediaType);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{webhookUrl}/api/matches/{matchId}/status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", PelipajaConfig.ApiSecret);
            request.Content = content;

            await _client.SendAsync(request);
            Console.WriteLine($"[Pelipaja] Posted status: {status}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[Pelipaja] Webhook request timed out for status: {status}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Pelipaja] Failed to post status '{status}': {e.Message}");
        }
    }
}
