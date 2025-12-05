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

    public static IServiceCollection AddWolfApi<T>(this IServiceCollection services) where T : Api
    {
        services.AddSingleton<T>()
            .AddHostedService(p => p.GetRequiredService<T>());
        return services;
    }
}