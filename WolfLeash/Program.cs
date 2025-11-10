using System.Net.Sockets;
using WolfLeash.Components;
using WolfLeash.Components.Classes;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables("WOLF_");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<HttpClient, HttpClientWrapper>(p =>
{
    return new HttpClientWrapper(new SocketsHttpHandler
    {
        ConnectCallback = async (ctx, token) =>
        {
            Console.WriteLine("Connecting...");
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
    });
});

builder.Services.AddSingleton<NSwagWolfApi.NSwagWolfApi>();
builder.Services.AddSingleton<NSwagDocker.NSwagDocker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();