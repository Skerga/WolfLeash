namespace WolfApi;
using Microsoft.Extensions.Logging;

public partial class Api
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var stream = await _httpClient.GetStreamAsync($"http://localhost/api/v1/events", cancellationToken);
                    var eventType = string.Empty;
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync(cancellationToken);
                        switch (line)
                        {
                            case null:
                            case ":keepalive":
                                continue;
                        }

                        if (line.StartsWith("event:"))
                            eventType = line["event: ".Length..];

                        if (!line.StartsWith("data:")) continue;
                        
                        var data = line["data: ".Length..];

                        FilterApiEvents(eventType, data);
                    }

                    _logger.LogError("Lost connection to the Wolf API SSE. End of Stream.");
                    SseConnectionLostEvent?.Invoke(false);
                    await Task.Delay(1000, cancellationToken);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError("The Wolf API SSE encountered an HttpRequestException exception: Statuscode: {statuscode} - {message}", 
                        e.StatusCode.ToString(),
                        e.Message);
                    SseConnectionLostEvent?.Invoke(true);
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    private void FilterApiEvents(string @event, string data)
    {
        ApiEvent?.Invoke(@event, data); //Push to Eventlog

        var operations = new Dictionary<string, Action<string>>
        {
            { "DockerPullImageEndEvent", (_) => { } },
            { "DockerPullImageStartEvent", (_) => { } },
            { "wolf::core::events::PlugDeviceEvent", (_) => { } },
            { "wolf::core::events::UnplugDeviceEvent", (_) => { } },
            { "wolf::core::events::PairSignal", (json) => { ClientPairRequestEvent?.Invoke(json); } },
            { "wolf::core::events::StartRunner", (_) => { } },
            { "wolf::core::events::StreamSession", (_) => { } },
            { "wolf::core::events::StopStreamEvent", (_) => { } },
            { "wolf::core::events::VideoSession", (_) => { } },
            { "wolf::core::events::RTPAudioPingEvent", (_) => { } },
            { "wolf::core::events::AudioSession", (_) => { } },
            { "wolf::core::events::IDRRequestEvent", (_) => { } },
            { "wolf::core::events::RTPVideoPingEvent", (_) => { } },
            { "wolf::core::events::ResumeStreamEvent", (_) => { } },
            { "wolf::core::events::PauseStreamEvent", (_) => { } },
            { "wolf::core::events::SwitchStreamProducerEvents", (_) => { } },
            { "wolf::core::events::JoinLobbyEvent", (_) => { } },
            { "wolf::core::events::LeaveLobbyEvent", (_) => { } },
            { "wolf::core::events::CreateLobbyEvent", (_) => { } },
            { "wolf::core::events::StopLobbyEvent", (_) => { } },
        };

        if (!operations.TryGetValue(@event, out var value))
        {
            _logger.LogWarning("{event} is not handled by WolfLeash, check for Updates", @event);
            return;
        }

        value(data);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public event IApiEventPublisher.ApiEventEventHandler? ApiEvent;

    public event IApiEventPublisher.ClientPairRequestEventHandler ClientPairRequestEvent;

    public event IApiEventPublisher.LobbyCreatedEventEventHandler? LobbyCreatedEvent;
    public event IApiEventPublisher.LobbyStoppedEventEventHandler? LobbyStoppedEvent;
    public event IApiEventPublisher.LobbyJoinEventEventHandler? LobbyJoinEvent;
    public event IApiEventPublisher.LobbyLeaveEventEventHandler? LobbyLeaveEvent;
    public event IApiEventPublisher.SseConnectionLostEventHandler? SseConnectionLostEvent;
    
    public event IApiEventPublisher.DockerPullingImageEventHandler? DockerPullingImageEvent;
    public event IApiEventPublisher.DockerPulledImageEventHandler? DockerPulledImageEvent;
}