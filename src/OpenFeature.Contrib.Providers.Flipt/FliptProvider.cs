using Grpc.Core;
using Grpc.Net.Client;
using OpenFeature.Error;
using OpenFeature.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Flipt.Evaluation.EvaluationService;
using EvaluationRequest = Flipt.Evaluation.EvaluationRequest;
using Metadata = OpenFeature.Model.Metadata;

namespace OpenFeature.Contrib.Providers.Flipt
{
    /// <summary>
    /// Flipt feature provider.
    /// </summary>
    public sealed class FliptProvider : FeatureProvider
    {
        private static readonly Metadata Metadata = new Metadata("Flipt");
        private readonly EvaluationServiceClient _client;
        private readonly FliptProviderConfiguration _configuration;

        /// <summary>
        /// Creates new instance of <see cref="FliptProvider" />
        /// </summary>
        /// <param name="configuration">Flipt provider options.</param>
        public FliptProvider(FliptProviderConfiguration configuration)
        {
            _configuration = configuration;
            var channel = GrpcChannel.ForAddress(configuration.ServiceUri);
            _client = new EvaluationServiceClient(channel);
        }

        /// <summary>
        /// Creates new instance of <see cref="FliptProvider" />
        /// </summary>
        /// <param name="evaluationServiceClient"><see cref="EvaluationServiceClient"/></param>
        /// <param name="configuration">Flipt provider options.</param>
        internal FliptProvider(EvaluationServiceClient evaluationServiceClient, FliptProviderConfiguration configuration)
        {
            _configuration = configuration;
            _client = evaluationServiceClient;
        }

        /// <inheritdoc />
        public override Metadata GetMetadata()
        {
            return Metadata;
        }

        /// <inheritdoc />
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null)
        {
            return _configuration.UseBooleanEvaluation
                ? ResolveBooleanAsync(flagKey, context)
                : ResolveVariantAsync(flagKey, defaultValue, context, bool.TryParse);
        }

        /// <inheritdoc />
        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null)
        {
            return ResolveVariantAsync(flagKey, defaultValue, context, AttachmentParser.TryParseDouble);
        }

        /// <inheritdoc />
        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null)
        {
            return ResolveVariantAsync(flagKey, defaultValue, context, AttachmentParser.TryParseInteger);
        }

        /// <inheritdoc />
        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null)
        {
            return ResolveVariantAsync(flagKey, defaultValue, context, AttachmentParser.TryParseString);
        }

        /// <inheritdoc />
        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null)
        {
            return ResolveVariantAsync(flagKey, defaultValue, context, AttachmentParser.TryParseJsonValue);
        }

        private async Task<ResolutionDetails<bool>> ResolveBooleanAsync(string flagKey, EvaluationContext context)
        {
            var request = FliptConverter.CreateRequest(flagKey, context, _configuration);
            var response = await SendRequestAsync(_client.BooleanAsync, request);
            return new ResolutionDetails<bool>(
                response.FlagKey,
                response.Enabled,
                reason: FliptConverter.ConvertReason(response.Reason));
        }

        internal async Task<ResolutionDetails<T>> ResolveVariantAsync<T>(string flagKey, T defaultValue,
            EvaluationContext context, TryParseDelegate<T> tryParse)
        {
            var request = FliptConverter.CreateRequest(flagKey, context, _configuration);
            var response = await SendRequestAsync(_client.VariantAsync, request);

            if (!response.Match)
                return new ResolutionDetails<T>(
                    response.FlagKey,
                    defaultValue,
                    reason: FliptConverter.ConvertReason(response.Reason));

            if (tryParse(response.VariantAttachment, out var value))
                return new ResolutionDetails<T>(
                    response.FlagKey,
                    value,
                    variant: response.VariantKey,
                    reason: FliptConverter.ConvertReason(response.Reason));
            throw new ParseErrorException(
                $"Can't convert value \"{response.VariantAttachment}\" to \"{typeof(T).Name}\" type");
        }

        internal static async Task<TResponse> SendRequestAsync<TResponse>(SendRequestDelegate<TResponse> sendRequest,
            EvaluationRequest request)
        {
            try
            {
                return await sendRequest(request);
            }
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.NotFound)
                {
                    throw new FlagNotFoundException(ex.Status.Detail);
                }

                if (ex.StatusCode == StatusCode.InvalidArgument)
                {
                    throw new InvalidContextException(ex.Status.Detail);
                }

                throw new GeneralException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new GeneralException(ex.Message, ex);
            }
        }

        internal delegate bool TryParseDelegate<T>(string value, out T result);

        internal delegate AsyncUnaryCall<T> SendRequestDelegate<T>(
            EvaluationRequest request,
            Grpc.Core.Metadata headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}