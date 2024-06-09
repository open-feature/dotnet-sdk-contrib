using AutoFixture;
using Flipt.Evaluation;
using FluentAssertions;
using Grpc.Core;
using NSubstitute;
using OpenFeature.Error;
using System;
using System.Threading.Tasks;
using Xunit;
using static Flipt.Evaluation.EvaluationService;

namespace OpenFeature.Contrib.Providers.Flipt.Test
{
    public class FliptProviderTest
    {
        private readonly IFixture _fixture = new Fixture();

        [Fact]
        public async Task SendRequest_NotFoundRpcException_ShouldThrowFlagNotFoundExceptionAsync()
        {
            // Arrange
            var errorDetail = _fixture.Create<string>();
            var request = _fixture.Create<EvaluationRequest>();
            var exception = new RpcException(new Status(StatusCode.NotFound, errorDetail));
            var sendRequestDelegate = Substitute.For<FliptProvider.SendRequestDelegate<VariantEvaluationResponse>>();
            sendRequestDelegate
                .When(x => x.Invoke(request))
                .Throw(exception);

            // Act
            var act = () => FliptProvider.SendRequestAsync(sendRequestDelegate, request);

            // Assert
            await act.Should().ThrowAsync<FlagNotFoundException>().WithMessage(errorDetail);
        }

        [Fact]
        public async Task SendRequest_InvalidArgumentRpcException_ShouldThrowInvalidContextExceptionAsync()
        {
            // Arrange
            var errorDetail = _fixture.Create<string>();
            var request = _fixture.Create<EvaluationRequest>();
            var exception = new RpcException(new Status(StatusCode.InvalidArgument, errorDetail));
            var sendRequestDelegate = Substitute.For<FliptProvider.SendRequestDelegate<VariantEvaluationResponse>>();
            sendRequestDelegate
                .When(x => x.Invoke(request))
                .Throw(exception);

            // Act
            var act = () => FliptProvider.SendRequestAsync(sendRequestDelegate, request);

            // Assert
            await act.Should().ThrowAsync<InvalidContextException>().WithMessage(errorDetail);
        }

        [Fact]
        public async Task SendRequest_GeneralRpcException_ShouldThrowGeneralExceptionAsync()
        {
            // Arrange
            var errorDetail = _fixture.Create<string>();
            var request = _fixture.Create<EvaluationRequest>();
            var exception = new RpcException(new Status(StatusCode.Unavailable, errorDetail));
            var sendRequestDelegate = Substitute.For<FliptProvider.SendRequestDelegate<VariantEvaluationResponse>>();
            sendRequestDelegate
                .When(x => x.Invoke(request))
                .Throw(exception);

            // Act
            var act = () => FliptProvider.SendRequestAsync(sendRequestDelegate, request);

            // Assert
            await act.Should().ThrowAsync<GeneralException>().WithMessage(exception.Message);
        }

        [Fact]
        public async Task SendRequest_GeneralException_ShouldThrowGeneralExceptionAsync()
        {
            // Arrange
            var request = _fixture.Create<EvaluationRequest>();
            var exception = new Exception();
            var sendRequestDelegate = Substitute.For<FliptProvider.SendRequestDelegate<VariantEvaluationResponse>>();
            sendRequestDelegate
                .When(x => x.Invoke(request))
                .Throw(exception);

            // Act
            var act = () => FliptProvider.SendRequestAsync(sendRequestDelegate, request);

            // Assert
            await act.Should().ThrowAsync<GeneralException>();
        }

        [Fact]
        public async Task ResolveBooleanValue_VariableEvaluationDoesNotMatch_ShouldReturnExpectedValue()
        {
            // Arrange
            var config = _fixture
                .Build<FliptProviderConfiguration>()
                .With(b => b.UseBooleanEvaluation, false)
                .Create();
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<bool>();
            var channel = Substitute.For<ChannelBase>(config.ServiceUri.OriginalString);
            var client = Substitute.For<EvaluationServiceClient>(channel);
            var response = _fixture
                .Build<VariantEvaluationResponse>()
                .With(b => b.Match, false)
                .Create();
            client
                .VariantAsync(Arg.Any<EvaluationRequest>())
                .Returns(new AsyncUnaryCall<VariantEvaluationResponse>(Task.FromResult(response), null, null, null, null));

            var provider = new FliptProvider(client, config);

            // Act
            var result = await provider.ResolveBooleanValue(flagKey, defaultValue);

            // Assert
            result.FlagKey.Should().Be(response.FlagKey);
            result.Value.Should().Be(defaultValue);
            result.Variant.Should().BeNull();
            result.Reason.Should().Be(FliptConverter.ConvertReason(response.Reason));
        }

        [Fact]
        public async Task ResolveBooleanValue_VariableEvaluationMatched_ShouldReturnExpectedValue()
        {
            // Arrange
            var config = _fixture
                .Build<FliptProviderConfiguration>()
                .With(b => b.UseBooleanEvaluation, false)
                .Create();
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<bool>();
            var value = _fixture.Create<bool>();
            var attachment = System.Text.Json.JsonSerializer.Serialize(value);
            var channel = Substitute.For<ChannelBase>(config.ServiceUri.OriginalString);
            var client = Substitute.For<EvaluationServiceClient>(channel);
            var response = _fixture
                .Build<VariantEvaluationResponse>()
                .With(b => b.Match, true)
                .With(b => b.VariantAttachment, attachment)
                .Create();
            client
                .VariantAsync(Arg.Any<EvaluationRequest>())
                .Returns(new AsyncUnaryCall<VariantEvaluationResponse>(Task.FromResult(response), null, null, null, null));

            var provider = new FliptProvider(client, config);

            // Act
            var result = await provider.ResolveBooleanValue(flagKey, defaultValue);

            // Assert
            result.FlagKey.Should().Be(response.FlagKey);
            result.Value.Should().Be(value);
            result.Reason.Should().Be(FliptConverter.ConvertReason(response.Reason));
        }

        [Fact]
        public async Task ResolveBooleanValue_BooleanEvaluation_ShouldReturnExpectedValue()
        {
            // Arrange
            var config = _fixture
                .Build<FliptProviderConfiguration>()
                .With(b => b.UseBooleanEvaluation, true)
                .Create();
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<bool>();
            var channel = Substitute.For<ChannelBase>(config.ServiceUri.OriginalString);
            var client = Substitute.For<EvaluationServiceClient>(channel);
            var response = _fixture.Create<BooleanEvaluationResponse>();
            client
                .BooleanAsync(Arg.Any<EvaluationRequest>())
                .Returns(new AsyncUnaryCall<BooleanEvaluationResponse>(Task.FromResult(response), null, null, null, null));

            var provider = new FliptProvider(client, config);

            // Act
            var result = await provider.ResolveBooleanValue(flagKey, defaultValue);

            // Assert
            result.Value.Should().Be(response.Enabled);
            result.FlagKey.Should().Be(response.FlagKey);
            result.Reason.Should().Be(FliptConverter.ConvertReason(response.Reason));
        }

        [Fact]
        public async Task ResolveBooleanValue_VariableEvaluationCanNotParseAttachment_ShouldReturnExpectedValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<bool>();
            var value = _fixture.Create<string>();
            var attachment = System.Text.Json.JsonSerializer.Serialize(value);
            var provider = CreateVariantProvider(attachment);

            // Act
            var act = () => provider.ResolveBooleanValue(flagKey, defaultValue);

            // Assert
            await act.Should().ThrowAsync<ParseErrorException>();
        }

        [Fact]
        public async Task ResolveDoubleValue_Matched_ShouldReturnExpectedValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<double>();
            var value = _fixture.Create<double>();
            var attachment = System.Text.Json.JsonSerializer.Serialize(value);
            var provider = CreateVariantProvider(attachment);

            // Act
            var result = await provider.ResolveDoubleValue(flagKey, defaultValue);

            // Assert
            result.Value.Should().Be(value);
        }

        [Fact]
        public async Task ResolveIntegerValue_Matched_ShouldReturnExpectedValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<int>();
            var value = _fixture.Create<int>();
            var attachment = System.Text.Json.JsonSerializer.Serialize(value);
            var provider = CreateVariantProvider(attachment);

            // Act
            var result = await provider.ResolveDoubleValue(flagKey, defaultValue);

            // Assert
            result.Value.Should().Be(value);
        }

        [Fact]
        public async Task ResolveStringValue_Matched_ShouldReturnExpectedValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var defaultValue = _fixture.Create<string>();
            var value = _fixture.Create<string>();
            var attachment = System.Text.Json.JsonSerializer.Serialize(value);
            var provider = CreateVariantProvider(attachment);

            // Act
            var result = await provider.ResolveStringValue(flagKey, defaultValue);

            // Assert
            result.Value.Should().Be(value);
        }

        private FliptProvider CreateVariantProvider(string attachment, bool match = true)
        {
            var config = _fixture
                .Build<FliptProviderConfiguration>()
                .With(b => b.UseBooleanEvaluation, false)
                .Create();
            var channel = Substitute.For<ChannelBase>(config.ServiceUri.OriginalString);
            var client = Substitute.For<EvaluationServiceClient>(channel);
            var response = _fixture
                .Build<VariantEvaluationResponse>()
                .With(b => b.Match, match)
                .With(b => b.VariantAttachment, attachment)
                .Create();
            client
                .VariantAsync(Arg.Any<EvaluationRequest>())
                .Returns(new AsyncUnaryCall<VariantEvaluationResponse>(Task.FromResult(response), null, null, null, null));
            return new FliptProvider(client, config);
        }
    }
}
