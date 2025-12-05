using NSwagWolfApi;

namespace WolfLeash.Components.Classes;
using WolfApi;

public class Api : WolfApi.Api
{
    public Api(WolfApiHttpClient httpClient, ILogger<WolfApi.Api> logger) : base(httpClient, logger) { }
    public Api(ILogger<WolfApi.Api> logger, IConfiguration configuration) : base(logger, configuration) { }

    private static Task Raise<TSource, TEventArgs>(Func<TSource, TEventArgs, Task>? handlers, TSource source, TEventArgs args)
    {
        if (handlers != null)
        {
            return Task.WhenAll(handlers.GetInvocationList()
                .OfType<Func<TSource, TEventArgs, Task>>()
                .Select(h => h(source, args)));
        }

        return Task.CompletedTask;
    }
    
    protected override async Task OnPairSignalEvent(string data)
    {
        await Raise(PairRequestEvent, this, data);
    }

    protected override async Task OnProfilesUpdatedEvent(ICollection<Profile> profiles)
    {
        await Raise(ProfilesUpdatedEvent, this, profiles);
    }

    public event Func<object, ICollection<Profile>, Task>? ProfilesUpdatedEvent;
    public event Func<object, string, Task>? PairRequestEvent;
}