namespace WolfApi;

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

public partial class Api
{
    private ConcurrentDictionary<string, bool>? _existingDockerImages;
    public ConcurrentDictionary<string, bool> ExistingDockerImages => _existingDockerImages ??= RequestStatusOfDockerImages();
    
    private ConcurrentDictionary<string, bool> RequestStatusOfDockerImages()
    {
        var profiles = TryAsync(WolfApi.ProfilesAsync()).Result?.Profiles ?? [];
        var tuples = profiles.SelectMany(p => p.Apps)
            .Select(a => a.Runner.Image)
            .Distinct()
            .Select(image => (
                image,
                TryAsync(DockerApi.ImageInspectAsync(image)).Result is not null)
            );

        var dict = new ConcurrentDictionary<string, bool>();
        foreach (var pair in tuples)
        {
            dict[pair.image] = pair.Item2;
        }

        return dict;
    }
    
    private sealed record PullImageResponse
    {
        [JsonPropertyName("success")]
        public bool? Success { get; init; }

        [JsonPropertyName("layer_id")]
        public string? LayerId { get; init; }

        [JsonPropertyName("current_progress")]
        public long CurrentProgress { get; init; }

        [JsonPropertyName("total")]
        public long Total { get; init; }
    }

    public void PullImage(string imageName)
    {
        Task.Run(async () =>
        {
            var json = $$"""
                         { "image_name": "{{imageName}}" }
                         """;
            
            await OnDockerImagePullProgressEvent(imageName, 0.0);

            _logger.LogInformation("Pulling image: {img}", imageName);

            var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"{BaseAddress}docker/images/pull")
            {
                Content = new StringContent(json)
            };

            var response = await _httpClient.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogInformation("Pull request: {httpcode}", response.StatusCode);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Cant Pull Image: {img}, {httpcode}", imageName, response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Dictionary<string, PullImageResponse> layerSizes = [];
            var isUnpacking = false;
            long lastCurrent = 0;
            var hasDownloaded = false;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    continue;

                var parsed = JsonSerializer.Deserialize<PullImageResponse>(line);
                if (parsed is null)
                    continue;

                if (parsed.Success is not null && parsed.Success == true)
                {
                    ExistingDockerImages[imageName] = true;

                    if (hasDownloaded)
                        await OnDockerImageUpdatedEvent(imageName);
                    else
                        await OnDockerImageAlreadyUptoDateEvent(imageName);

                    _logger.LogInformation("Image: {img} {status}", imageName,
                        hasDownloaded ? "was Updated" : "is already up to Date");
                    return;
                }

                hasDownloaded = true;

                if (parsed.LayerId is null)
                    continue;

                layerSizes[parsed.LayerId] = new PullImageResponse
                {
                    LayerId = parsed.LayerId,
                    CurrentProgress = parsed.CurrentProgress,
                    Total = parsed.Total
                };

                long current = 0;
                long total = 0;

                foreach (var kv in layerSizes)
                {
                    current += kv.Value.CurrentProgress;
                    total += kv.Value.Total;
                }

                var sizeTotal = total;
                total *= 2;

                if (total <= 1000) continue;

                if (lastCurrent > 0 && lastCurrent > current + 0.3 * lastCurrent)
                    isUnpacking = true;
                lastCurrent = current;
                var percentProgress = 100.0 * (current + (isUnpacking ? sizeTotal : 0)) / total;

                await OnDockerImagePullProgressEvent(imageName, percentProgress);
            }
        });
    }
    
    public static async Task<T?> TryAsync<T>(Task<T> task)
    {
        try
        {
            return await task;
        }
        catch (Exception)
        {
            return default;
        }
    }

    protected virtual Task OnDockerImageUpdatedEvent(string imageName)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDockerImageAlreadyUptoDateEvent(string imageName)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDockerImagePullProgressEvent(string imageName, double progress)
    {
        return Task.CompletedTask;
    }
}