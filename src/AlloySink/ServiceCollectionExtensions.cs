using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlloySink(this IServiceCollection services, AlloySinkOptions options)
    {
        services.TryAddSingleton<IAlloyClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<AlloyClient>>();
            return new AlloyClient(options, logger);
        });

        services.TryAddSingleton<IAlloySink>(provider =>
        {
            var alloyClient = provider.GetRequiredService<IAlloyClient>();
            var logger = provider.GetRequiredService<ILogger<AlloySink>>();
            return new AlloySink(options, alloyClient, logger);
        });

        return services;
    }

    public static IServiceCollection AddAlloySink(this IServiceCollection services, Action<AlloySinkOptions> configureOptions)
    {
        var options = new AlloySinkOptions();
        configureOptions(options);
        return services.AddAlloySink(options);
    }

    public static IServiceCollection AddAlloySink(this IServiceCollection services, string alloyEndpoint, string serviceName, string serviceVersion = "1.0.0", string environment = "production")
    {
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = alloyEndpoint,
            ServiceName = serviceName,
            ServiceVersion = serviceVersion,
            Environment = environment
        };
        return services.AddAlloySink(options);
    }
}