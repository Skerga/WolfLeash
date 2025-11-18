using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using WolfApi;
using WolfLeash.Components;
using WolfLeash.Components.Classes;
using WolfLeash.Database;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables("WOLF_");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddTransient<ColorGenerator>();
builder.Services.AddWolfApi();
builder.Services.AddDbContext<WolfLeashDbContext>();

builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<EventLogger>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    
    // Update Database
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WolfLeashDbContext>();
    await db.Database.MigrateAsync();
}
else
{
    // Recreate Database for Development
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WolfLeashDbContext>();
    db.Database.EnsureDeleted();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();