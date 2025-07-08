using Xunit;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public class AlloySinkOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new AlloySinkOptions();

        // Assert
        Assert.Equal("http://localhost:4318", options.AlloyEndpoint);
        Assert.Equal("scraper-service", options.ServiceName);
        Assert.Equal("1.0.0", options.ServiceVersion);
        Assert.Equal("development", options.Environment);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(5), options.BatchInterval);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(TimeSpan.FromSeconds(1), options.RetryDelay);
        Assert.True(options.EnableBatching);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var options = new AlloySinkOptions();

        // Act
        options.AlloyEndpoint = "http://custom-endpoint:4318";
        options.ServiceName = "custom-service";
        options.ServiceVersion = "2.0.0";
        options.Environment = "production";
        options.BatchSize = 50;
        options.BatchInterval = TimeSpan.FromSeconds(10);
        options.MaxRetries = 5;
        options.RetryDelay = TimeSpan.FromSeconds(2);
        options.EnableBatching = false;

        // Assert
        Assert.Equal("http://custom-endpoint:4318", options.AlloyEndpoint);
        Assert.Equal("custom-service", options.ServiceName);
        Assert.Equal("2.0.0", options.ServiceVersion);
        Assert.Equal("production", options.Environment);
        Assert.Equal(50, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(10), options.BatchInterval);
        Assert.Equal(5, options.MaxRetries);
        Assert.Equal(TimeSpan.FromSeconds(2), options.RetryDelay);
        Assert.False(options.EnableBatching);
    }

    [Theory]
    [InlineData("http://localhost:4318")]
    [InlineData("https://alloy.example.com:4318")]
    [InlineData("http://192.168.1.100:4318")]
    public void AlloyEndpoint_AcceptsValidUrls(string endpoint)
    {
        // Arrange
        var options = new AlloySinkOptions();

        // Act & Assert
        options.AlloyEndpoint = endpoint;
        Assert.Equal(endpoint, options.AlloyEndpoint);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public void BatchSize_AcceptsPositiveValues(int batchSize)
    {
        // Arrange
        var options = new AlloySinkOptions();

        // Act & Assert
        options.BatchSize = batchSize;
        Assert.Equal(batchSize, options.BatchSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void MaxRetries_AcceptsNonNegativeValues(int maxRetries)
    {
        // Arrange
        var options = new AlloySinkOptions();

        // Act & Assert
        options.MaxRetries = maxRetries;
        Assert.Equal(maxRetries, options.MaxRetries);
    }
}