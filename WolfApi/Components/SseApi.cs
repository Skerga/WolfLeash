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
                    var stream = await _httpClient.GetStreamAsync($"{BaseAddress}events", cancellationToken);
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

                        await FilterApiEvents(eventType, data);
                    }

                    _logger.LogError("Lost connection to the Wolf API SSE. End of Stream.");
                    await OnSseConnectionLostEvent(false);
                    await Task.Delay(1000, cancellationToken);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError("The Wolf API SSE encountered an HttpRequestException exception: Statuscode: {statuscode} - {message}", 
                        e.StatusCode.ToString(),
                        e.Message);
                    await OnSseConnectionLostEvent(true);
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    private async Task FilterApiEvents(string @event, string data)
    {
        await OnEvent(@event, data);

        var operations = new Dictionary<string, Func<string, Task>>
        {
            { "DockerPullImageEndEvent", OnDockerPullImageEndEvent },
            { "DockerPullImageStartEvent", OnDockerPullImageStartEvent },
            { "wolf::core::events::PlugDeviceEvent", OnPlugDeviceEvent },
            { "wolf::core::events::UnplugDeviceEvent", OnUnplugDeviceEvent },
            { "wolf::core::events::PairSignal", OnPairSignalEvent },
            { "wolf::core::events::StartRunner", OnStartRunnerEvent },
            { "wolf::core::events::StreamSession", OnStreamSessionEvent },
            { "wolf::core::events::StopStreamEvent", OnStopStreamEvent },
            { "wolf::core::events::VideoSession", OnVideoSessionEvent },
            { "wolf::core::events::RTPAudioPingEvent", OnRTPAudioPingEvent },
            { "wolf::core::events::AudioSession", OnAudioSessionEvent },
            { "wolf::core::events::IDRRequestEvent", OnIDRRequestEvent },
            { "wolf::core::events::RTPVideoPingEvent", OnRTPVideoPingEvent },
            { "wolf::core::events::ResumeStreamEvent", OnResumeStreamEvent },
            { "wolf::core::events::PauseStreamEvent", OnPauseStreamEvent },
            { "wolf::core::events::SwitchStreamProducerEvents", OnSwitchStreamProducerEvents },
            { "wolf::core::events::JoinLobbyEvent", OnJoinLobbyEvent },
            { "wolf::core::events::LeaveLobbyEvent", OnLeaveLobbyEvent },
            { "wolf::core::events::CreateLobbyEvent", OnCreateLobbyEvent },
            { "wolf::core::events::StopLobbyEvent", OnStopLobbyEvent },
        };

        if (!operations.TryGetValue(@event, out var value))
        {
            _logger.LogWarning("{event} is not handled by this Client version, check for Updates", @event);
            return;
        }

        await value(data);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnSseConnectionLostEvent(bool isFatal)
    {
        return Task.CompletedTask;
    }
    
    protected virtual Task OnEvent(string @event, string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDockerPullImageEndEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDockerPullImageStartEvent(string data)
    {
        return Task.CompletedTask;
    }
    
    protected virtual Task OnPlugDeviceEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUnplugDeviceEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnPairSignalEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStartRunnerEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStreamSessionEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStopStreamEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnVideoSessionEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnRTPAudioPingEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnAudioSessionEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnIDRRequestEvent(string data)
    {
        return Task.CompletedTask;
    }
    
    protected virtual Task OnRTPVideoPingEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnResumeStreamEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnPauseStreamEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnSwitchStreamProducerEvents(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnJoinLobbyEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnLeaveLobbyEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnCreateLobbyEvent(string data)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStopLobbyEvent(string data)
    {
        return Task.CompletedTask;
    }
}