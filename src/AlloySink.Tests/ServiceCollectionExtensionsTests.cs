using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAlloySink_WithOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:4318",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
        };

        // Act
        services.AddAlloySink(options);
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var alloySink = serviceProvider.GetService<IAlloySink>();
        var alloyClient = serviceProvider.GetService<IAlloyClient>();

        Assert.NotNull(alloySink);
        Assert.NotNull(alloyClient);
        Assert.IsType<Deneblab.AlloySink.AlloySink>(alloySink);
        Assert.IsType<Deneblab.AlloySink.AlloyClient>(alloyClient);
    }

    [Fact]
    public void AddAlloySink_WithConfigureAction_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAlloySink(options =>
        {
            options.AlloyEndpoint = "http://localhost:4318";
            options.ServiceName = "test-service";
            options.ServiceVersion = "1.0.0";
            options.Environment = "test";
            options.EnableBatching = true;
            options.BatchSize = 50;
        });

        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var alloySink = serviceProvider.GetService<IAlloySink>();
        var alloyClient = serviceProvider.GetService<IAlloyClient>();

        Assert.NotNull(alloySink);
        Assert.NotNull(alloyClient);
    }

    [Fact]
    public void AddAlloySink_WithSimpleParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAlloySink(
            alloyEndpoint: "http://localhost:4318",
            serviceName: "test-service",
            serviceVersion: "2.0.0",
            environment: "production"
        );

        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var alloySink = serviceProvider.GetService<IAlloySink>();
        var alloyClient = serviceProvider.GetService<IAlloyClient>();

        Assert.NotNull(alloySink);
        Assert.NotNull(alloyClient);
    }

    [Fact]
    public void AddAlloySink_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAlloySink(options =>
        {
            options.AlloyEndpoint = "http://localhost:4318";
            options.ServiceName = "test-service";
        });

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var alloySink1 = serviceProvider.GetService<IAlloySink>();
        var alloySink2 = serviceProvider.GetService<IAlloySink>();
        var alloyClient1 = serviceProvider.GetService<IAlloyClient>();
        var alloyClient2 = serviceProvider.GetService<IAlloyClient>();

        // Assert
        Assert.Same(alloySink1, alloySink2);
        Assert.Same(alloyClient1, alloyClient2);
    }

    [Fact]
    public async Task AddAlloySink_IntegrationTest_CanLogMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAlloySink(options =>
        {
            options.AlloyEndpoint = "http://localhost:9999"; // Non-existent endpoint
            options.ServiceName = "test-service";
            options.ServiceVersion = "1.0.0";
            options.Environment = "test";
            options.EnableBatching = false;
            options.MaxRetries = 1;
            options.RetryDelay = TimeSpan.FromMilliseconds(10);
        });

        using var serviceProvider = services.BuildServiceProvider();
        var alloySink = serviceProvider.GetRequiredService<IAlloySink>();

        // Act & Assert - Should not throw
        await alloySink.LogInfoAsync("Test message from DI");
        await alloySink.LogErrorAsync("Test error", new Exception("Test exception"));
        await alloySink.FlushAsync();
    }
}