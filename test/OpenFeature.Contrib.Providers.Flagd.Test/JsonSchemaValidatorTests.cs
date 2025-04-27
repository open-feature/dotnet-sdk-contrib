using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class JsonSchemaValidatorTests
    {
        private readonly string _targetingSchemaJson = @"
            {
                ""$id"": ""https://example.com/example.schema.json"",
                ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
                ""description"": ""A sample Schema"",
                ""properties"": {
                    ""id"": {
                        ""description"": ""The unique identifier"",
                        ""type"": ""integer""
                    }
                },
                ""title"": ""Targeting"",
                ""type"": ""object""
            }
        ";

        private readonly string _flagsSchemaJson = @"
            {
                ""$id"": ""https://example.com/example2.schema.json"",
                ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
                ""description"": ""A 2nd Sample Schema"",
                ""properties"": {
                    ""name"": {
                        ""description"": ""The name"",
                        ""type"": ""string""
                    }
                },
                ""title"": ""Flags"",
                ""type"": ""object""
            }
        ";

        [Fact]
        public async Task InitializeFetchesFlagSchema()
        {
            // Arrange
            var targetingResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_targetingSchemaJson, Encoding.UTF8, "application/json")
            };
            var flagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_flagsSchemaJson, Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new MockHttpMessageHandler(targetingResponse, flagsResponse))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

            // Act
            await validator.InitializeAsync();

            // Assert
            var logs = logger.Collector.GetSnapshot();
            Assert.Empty(logs);
        }

        [Fact]
        public async Task InitializeFailsOnTargetingSchemaLogsWarning()
        {
            // Arrange
            var targetingResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var flagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_flagsSchemaJson, Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new MockHttpMessageHandler(targetingResponse, flagsResponse))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

            // Act
            await validator.InitializeAsync();

            // Assert
            var logs = logger.Collector.GetSnapshot();
            Assert.Single(logs);
            Assert.Multiple(() =>
            {
                var actual = logs[0];
                Assert.Equal(LogLevel.Warning, actual.Level);
                Assert.Equal("Unable to retrieve Flagd targeting JSON Schema, status code: InternalServerError", actual.Message);
            });
        }

        [Fact]
        public async Task InitializeFailsOnFlagsSchemaLogsWarning()
        {
            // Arrange
            var targetingResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_targetingSchemaJson, Encoding.UTF8, "application/json")
            };
            var flagsResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(_flagsSchemaJson, Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new MockHttpMessageHandler(targetingResponse, flagsResponse))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

            // Act
            await validator.InitializeAsync();

            // Assert
            var logs = logger.Collector.GetSnapshot();
            Assert.Single(logs);
            Assert.Multiple(() =>
            {
                var actual = logs[0];
                Assert.Equal(LogLevel.Warning, actual.Level);
                Assert.Equal("Unable to retrieve Flagd flags JSON Schema, status code: InternalServerError", actual.Message);
            });
        }

        [Fact]
        public async Task InitializeInvalidJsonSchemaLogsError()
        {
            // Arrange
            var targetingResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"<xml/>", Encoding.UTF8, "application/json")
            };
            var flagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_flagsSchemaJson, Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new MockHttpMessageHandler(targetingResponse, flagsResponse))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

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
        public async Task ValidateSchemaNoWarnings()
        {
            // Arrange
            var targetingResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_targetingSchemaJson, Encoding.UTF8, "application/json")
            };
            var flagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_flagsSchemaJson, Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new MockHttpMessageHandler(targetingResponse, flagsResponse))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

            await validator.InitializeAsync();

            // Act
            var configuration = @"{""$schema"":""https://example.com/example2.schema.jsonhttps://example.com/example2.schema.json"",""name"":""test""}";
            validator.Validate(configuration);

            // Assert
            var logs = logger.Collector.GetSnapshot();
            Assert.Empty(logs);
        }

        [Fact]
        public async Task ValidateSchemaInvalidJsonWarning()
        {
            // Arrange
            var targetingResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_targetingSchemaJson, Encoding.UTF8, "application/json")
            };
            var flagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_flagsSchemaJson, Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new MockHttpMessageHandler(targetingResponse, flagsResponse))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

            await validator.InitializeAsync();

            // Act
            var configuration = @"{""$schema"":""https://example.com/example2.schema.jsonhttps://example.com/example2.schema.json"",""name"":15}";
            validator.Validate(configuration);

            // Assert
            var logs = logger.Collector.GetSnapshot();
            Assert.Single(logs);
            Assert.Multiple(() =>
            {
                var actual = logs[0];
                Assert.Equal(LogLevel.Warning, actual.Level);
                Assert.StartsWith("Validating Flagd configuration resulted in Schema Validation errors", actual.Message);
            });
        }

        [Fact]
        public void WhenNotInitializedThenValidateSchemaNoWarnings()
        {
            // Arrange
            var httpClient = new HttpClient(new MockHttpMessageHandler(null, null))
            {
                BaseAddress = new Uri("https://example.com")
            };
            var logger = new FakeLogger<JsonSchemaValidatorTests>();
            var validator = new JsonSchemaValidator(httpClient, logger);

            // Act
            var configuration = @"{""$schema"":""https://example.com/example2.schema.jsonhttps://example.com/example2.schema.json"",""name"":""test""}";
            validator.Validate(configuration);

            // Assert
            var logs = logger.Collector.GetSnapshot();
            Assert.Empty(logs);
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _targetingResponse;
        private readonly HttpResponseMessage _flagsResponse;

        public MockHttpMessageHandler(HttpResponseMessage targetingResponse, HttpResponseMessage flagsResponse)
        {
            _targetingResponse = targetingResponse;
            _flagsResponse = flagsResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.PathAndQuery.Contains("/schema/v0/targeting.json"))
            {
                return Task.FromResult(_targetingResponse);
            }

            if (request.RequestUri.PathAndQuery.Contains("/schema/v0/flags.json"))
            {
                return Task.FromResult(_flagsResponse);
            }

            throw new NotImplementedException("HttpMessageHandler not implemented");
        }
    }
}
