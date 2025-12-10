namespace WolfApi;

public partial class Api
{
    public async Task<ICollection<NSwagWolfApi.App>> GetAppsAsync() 
        => (await WolfApi.AppsAsync()).Apps ?? Array.Empty<NSwagWolfApi.App>();
    
    public async Task<ICollection<NSwagWolfApi.PairedClient>> GetClientsAsync() 
        => (await WolfApi.ClientsAsync()).Clients ?? Array.Empty<NSwagWolfApi.PairedClient>();

    public async Task<NSwagDocker.ImageInspect> GetImageInspectAsync(string imageName)
        => await DockerApi.ImageInspectAsync(imageName);
    
    public async Task<ICollection<NSwagWolfApi.Lobby>> GetLobbiesAsync() 
        => (await WolfApi.LobbiesAsync()).Lobbies ?? Array.Empty<NSwagWolfApi.Lobby>();
    
    public async Task<ICollection<NSwagWolfApi.PendingPairClient>> GetPendingPairRequestsAsync() 
        => (await WolfApi.PendingAsync()).Requests ?? Array.Empty<NSwagWolfApi.PendingPairClient>();
    
    public async Task<ICollection<NSwagWolfApi.Profile>> GetProfilesAsync() 
        => (await WolfApi.ProfilesAsync()).Profiles ?? Array.Empty<NSwagWolfApi.Profile>();
    
    public async Task<ICollection<NSwagWolfApi.StreamSession>> GetSessionsAsync()
        => (await WolfApi.SessionsAsync()).Sessions ?? Array.Empty<NSwagWolfApi.StreamSession>();


    public async Task<NSwagWolfApi.GenericSuccessResponse> PostAppsAddAsync(NSwagWolfApi.App app)
        => await WolfApi.AddAsync(app);

    public async Task<NSwagWolfApi.GenericSuccessResponse> PostAppsDeleteAsync(NSwagWolfApi.App app)
        => await WolfApi.DeleteAsync(new NSwagWolfApi.AppDeleteRequest(){Id = app.Id});
    
    public async Task<NSwagWolfApi.GenericSuccessResponse> PostAppsDeleteAsync(string appId)
        => await WolfApi.DeleteAsync(new NSwagWolfApi.AppDeleteRequest(){Id = appId});

    public async Task<NSwagWolfApi.GenericSuccessResponse> PostClientSettingsAsync(
        NSwagWolfApi.UpdateClientSettingsRequest clientSettings)
        => await WolfApi.SettingsAsync(clientSettings);
    
    public async Task<NSwagWolfApi.LobbyCreateResponse> PostLobbiesCreate(NSwagWolfApi.CreateLobbyRequest lobby)
        => await WolfApi.CreateAsync(lobby);
}