using Microsoft.Extensions.DependencyInjection;

namespace WolfApi;

public static class Extensions
{
    public static IServiceCollection AddWolfApi(this IServiceCollection services)
    {
        services.AddSingleton<Api>()
            .AddHostedService(p => p.GetRequiredService<Api>());

        return services;
    }
}