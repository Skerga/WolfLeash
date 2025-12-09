namespace WolfApi;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using NSwagWolfApi;
using NSwagDocker;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

public partial class Api : IHostedService
{
    public NSwagDocker DockerApi { get; }
    public NSwagWolfApi WolfApi { get; }
    private readonly ILogger<Api> _logger;
    private readonly HttpClient _httpClient;

    public string WolfSocketPath { get; set; } = "unix:///etc/wolf/cfg/wolf.sock";
    public string BaseAddress { get; set; } = "http://localhost/api/v1/";
    
    public ConcurrentQueue<Profile>? Profiles { get; private set; }

    public Api() : this(NullLogger<Api>.Instance) { }
    
    public Api(ILogger<Api> logger) : 
        this(logger, 
            new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"SOCKET_PATH", "/etc/wolf/cfg/wolf.sock"}
            }!).Build())
    { }
    
    public Api(IConfiguration config) : this(NullLogger<Api>.Instance, config) { }
    
    public Api(ILogger<Api> logger, IConfiguration configuration)
    {
        var socketPath = configuration.GetValue<string?>("SOCKET_PATH") ?? WolfSocketPath;
        BaseAddress = configuration.GetValue<string?>("BASE_ADDRESS") ?? BaseAddress;
        
        var options = new TokenBucketRateLimiterOptions
        { 
            TokenLimit = 8, 
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 3, 
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1), 
            TokensPerPeriod = 2, 
            AutoReplenishment = true
        };
        
        if (socketPath.StartsWith("unix://"))
        {
            socketPath = socketPath["unix://".Length..];
            _httpClient = new HttpClient(
                handler: new ClientSideRateLimitedHandler(
                    limiter: new TokenBucketRateLimiter(options),
                    httpMessageHandler: new SocketsHttpHandler
                    {
                        ConnectCallback = async (_, token) =>
                        {
                            if (!Path.Exists(socketPath))
                            {
                                throw new FileNotFoundException(socketPath);
                            }

                            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                            var endpoint = new UnixDomainSocketEndPoint(socketPath);
                            await socket.ConnectAsync(endpoint, token);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                    }
                )
            );
        }
        else
        {
            _httpClient = new HttpClient(
                handler: new ClientSideRateLimitedHandler(
                    limiter: new TokenBucketRateLimiter(options),
                    httpMessageHandler: new HttpClientHandler()
                )
            );
        }
        

        
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
        await OnProfilesUpdatedEvent(profiles);
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

    //public event IApiEventPublisher.ProfilesUpdatedEventHandler? ProfilesUpdatedEvent;

    protected virtual Task OnProfilesUpdatedEvent(ICollection<Profile> profiles)
    {
        return Task.CompletedTask;
    }
    
}