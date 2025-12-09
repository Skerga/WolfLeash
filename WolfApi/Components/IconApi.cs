namespace WolfApi;

using System.Net;
using Microsoft.Extensions.Logging;
using NSwagWolfApi;

public partial class Api
{
    public async Task<Stream?> GetIcon(App app, int retry = 0)
    {
        if (app.Icon_png_path is not null && app.Icon_png_path != "") // image set, get image from wolf
            return await GetIcon(app.Icon_png_path, retry);

        if (app.Runner?.Image is null || !app.Runner.Image.Contains("ghcr.io/games-on-whales/"))
            return null;

        var name = app.Runner.Image["ghcr.io/games-on-whales/".Length..];
        var idx = name.LastIndexOf(':');
        if (idx >= 0)
            name = name[..idx];

        return await GetIcon($"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png", retry);
    }
    
    private async Task<Stream?> GetIcon(string iconPath, int retry = 0)
    {
        if (retry >= 5)
        {
            _logger.LogError("Failed Loading {icon} 5 times, skipping", iconPath);
            return null;
        }

        _logger.LogInformation("Requesting icon: {icon}", iconPath);

        HttpResponseMessage message;
        try
        {
            message = await _httpClient.GetAsync($"{BaseAddress}utils/get-icon?icon_path={iconPath}");
        }
        catch (HttpRequestException e)
        {
            if(e.InnerException is not null)
                _logger.LogWarning("Icon {icon} could not be accessed: {msg} - {exception} Retrying", iconPath, e.Message, e.InnerException.Message);
            else
                _logger.LogWarning("Icon {icon} could not be accessed: {msg} Retrying", iconPath, e.Message);
            return await GetIcon(iconPath, retry + 1);
        }

        if (message.StatusCode == HttpStatusCode.OK)
        {
            return await message.Content.ReadAsStreamAsync();
        }
        
        _logger.LogError("Could not access image: {icon}: {code}", iconPath, message.StatusCode);
        return null;
    }
}