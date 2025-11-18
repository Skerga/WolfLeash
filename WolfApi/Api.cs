using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

namespace WolfApi;

using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using NSwagWolfApi;
using NSwagDocker;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public partial class Api : IHostedService, IApiEventPublisher
{
    public NSwagDocker DockerApi { get; }
    public NSwagWolfApi WolfApi { get; }
    private readonly ILogger<Api> _logger;
    private readonly HttpClient _httpClient;

    public ConcurrentQueue<Profile>? Profiles { get; private set; }
    
    
    // ReSharper disable once ConvertToPrimaryConstructor
    public Api(WolfApiHttpClient httpClient, ILogger<Api> logger)
    {
        DockerApi = new(httpClient);
        WolfApi = new(httpClient);
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public Api(ILogger<Api> logger, IConfiguration configuration)
    {
        configuration.GetValue<string>("SOCKET_PATH");
        
        var options = new TokenBucketRateLimiterOptions
        { 
            TokenLimit = 8, 
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 3, 
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1), 
            TokensPerPeriod = 2, 
            AutoReplenishment = true
        };
        
        _httpClient = new HttpClient(
            handler: new ClientSideRateLimitedHandler(
                limiter: new TokenBucketRateLimiter(options),
                httpMessageHandler: new SocketsHttpHandler
                {
                    ConnectCallback = async (_, token) =>
                    {
                        var endpointPath = Environment.GetEnvironmentVariable("WOLF_SOCKET_PATH") ??
                                           "/etc/wolf/cfg/wolf.sock";

                        if (!Path.Exists(endpointPath))
                        {
                            throw new FileNotFoundException(endpointPath);
                        }

                        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                        var endpoint = new UnixDomainSocketEndPoint(endpointPath);
                        await socket.ConnectAsync(endpoint, token);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                }
            )
        );
        
        DockerApi = new(_httpClient);
        WolfApi = new(_httpClient);
        _logger = logger;
    }
    
    [MemberNotNull(nameof(Profiles))]
    public async Task UpdateProfiles()
    {
        Profiles = new ConcurrentQueue<Profile>();
        var profiles = await GetProfiles();
        foreach (var profile in profiles)
        {
            Profiles.Enqueue(profile);
        }
        ProfilesUpdatedEvent?.Invoke(Profiles.ToList());
    }
    
    public async Task<GenericSuccessResponse> AddProfile(Profile profile)
    {
        var val = await WolfApi.Add2Async(profile);
        await UpdateProfiles();
        return val;
    }

    public async Task<GenericSuccessResponse> DeleteProfile(Profile profile)
        => await DeleteProfile(profile.Id);

    public async Task<GenericSuccessResponse> DeleteProfile(string id)
    {
        var val = await WolfApi.RemoveAsync(new ProfileRemoveRequest()
        {
            Id = id
        });
        await UpdateProfiles();
        return val;
    }

    public async Task<GenericSuccessResponse> UpdateProfile(string id, Profile profile)
    {
        if(id != profile.Id) throw new ArgumentException("id must be equal to profile id", nameof(id));
        var del = await WolfApi.RemoveAsync(new ProfileRemoveRequest()
        {
            Id = id
        });
        if (!del.Success) return del;
        var add = await WolfApi.Add2Async(profile);
        // Todo: If Delete Succeeds and Add Fails, Try rescue Profile by some means, or wait for a Dedicated Update endpoint.
        await UpdateProfiles();
        return add;
    }

    public async Task<ICollection<Profile>> GetProfiles() =>
        (await WolfApi.ProfilesAsync()).Profiles ?? Array.Empty<Profile>();

    public event IApiEventPublisher.ProfilesUpdatedEventHandler? ProfilesUpdatedEvent;
}