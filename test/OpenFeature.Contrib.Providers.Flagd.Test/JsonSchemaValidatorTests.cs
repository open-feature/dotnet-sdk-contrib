using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class JsonSchemaValidatorTests
{
    [Fact]
    public async Task InitializeFetchesFlagSchema()
    {
        // Arrange
        var logger = new FakeLogger<JsonSchemaValidatorTests>();
        var validator = new JsonSchemaValidator(logger);

        // Act
        await validator.InitializeAsync();

        // Assert
        var logs = logger.Collector.GetSnapshot();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task InitializeWhenReadTargetingSchemaAsyncThrowsLogsError()
    {
        // Arrange
        var logger = new FakeLogger<JsonSchemaValidatorTests>();
        var failingSchemaProvider = Substitute.For<IFlagdJsonSchemaProvider>();
        var validator = new JsonSchemaValidator(logger, failingSchemaProvider);

        failingSchemaProvider.ReadTargetingSchemaAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("Simulated failure"));

        // Act
        await validator.InitializeAsync();

        // Assert
        var logs = logger.Collector.GetSnapshot();
        Assert.Single(logs);
        Assert.Multiple(() =>
        {
            var actual = logs[0];
            Assert.Equal(LogLevel.Error, actual.Level);
            Assert.Equal("Unable to retrieve Flagd flags and targeting JSON Schemas", actual.Message);
        });
    }

    [Fact]
    public async Task InitializeWhenReadFlagSchemaAsyncThrowsLogsError()
    {
        // Arrange
        var logger = new FakeLogger<JsonSchemaValidatorTests>();
        var failingSchemaProvider = Substitute.For<IFlagdJsonSchemaProvider>();
        var validator = new JsonSchemaValidator(logger, failingSchemaProvider);

        failingSchemaProvider.ReadTargetingSchemaAsync(Arg.Any<CancellationToken>())
            .Returns("{$id\": \"https://flagd.dev/schema/v0/targeting.json\"}");

        failingSchemaProvider.ReadFlagSchemaAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("Simulated failure"));

        // Act
        await validator.InitializeAsync();

        // Assert
        var logs = logger.Collector.GetSnapshot();
        Assert.Single(logs);
        Assert.Multiple(() =>
        {
            var actual = logs[0];
            Assert.Equal(LogLevel.Error, actual.Level);
            Assert.Equal("Unable to retrieve Flagd flags and targeting JSON Schemas", actual.Message);
        });
    }

    [Fact]
    public void WhenNotInitializedThenValidateSchemaNoWarnings()
    {
        // Arrange
        var logger = new FakeLogger<JsonSchemaValidatorTests>();
        var validator = new JsonSchemaValidator(logger);

        // Act
        var configuration = @"{""$schema"":""https://example.com/example2.schema.jsonhttps://example.com/example2.schema.json"",""name"":""test""}";
        validator.Validate(configuration);

        // Assert
        var logs = logger.Collector.GetSnapshot();
        Assert.Empty(logs);
    }
}
