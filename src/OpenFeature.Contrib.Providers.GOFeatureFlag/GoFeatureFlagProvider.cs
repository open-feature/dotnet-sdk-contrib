using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.GOFeatureFlag.converters;
using OpenFeature.Contrib.Providers.GOFeatureFlag.exception;
using OpenFeature.Contrib.Providers.GOFeatureFlag.extensions;
using OpenFeature.Contrib.Providers.GOFeatureFlag.hooks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.models;
using OpenFeature.Model;
using ZiggyCreatures.Caching.Fusion;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    /// <summary>
    ///     GoFeatureFlagProvider is the OpenFeature provider for GO Feature Flag.
    /// </summary>
    public class GoFeatureFlagProvider : FeatureProvider
    {
        private const string ApplicationJson = "application/json";
        private ExporterMetadata _exporterMetadata;
        private HttpClient _httpClient;

        private IFusionCache _cache = null;

        /// <summary>
        ///     Constructor of the provider.
        ///     <param name="options">Options used while creating the provider</param>
        ///     <exception cref="InvalidOption">if no options are provided or we have a wrong configuration.</exception>
        /// </summary>
        public GoFeatureFlagProvider(GoFeatureFlagProviderOptions options)
        {
            ValidateInputOptions(options);
            InitializeProvider(options);
        }

        /// <summary>
        ///     List of hooks to use for this provider
        /// </summary>
        /// <returns></returns>
        public override IImmutableList<Hook> GetProviderHooks()
        {
            var hooks = ImmutableArray.CreateBuilder<Hook>();
            hooks.Add(new EnrichEvaluationContextHook(_exporterMetadata));
            return hooks.ToImmutable();
        }

        /// <summary>
        ///     validateInputOptions is validating the different options provided when creating the provider.
        /// </summary>
        /// <param name="options">Options used while creating the provider</param>
        /// <exception cref="InvalidOption">if no options are provided or we have a wrong configuration.</exception>
        private void ValidateInputOptions(GoFeatureFlagProviderOptions options)
        {
            if (options is null) throw new InvalidOption("No options provided");

            if (string.IsNullOrEmpty(options.Endpoint))
                throw new InvalidOption("endpoint is a mandatory field when initializing the provider");
        }

        /// <summary>
        ///     initializeProvider is initializing the different class element used by the provider.
        /// </summary>
        /// <param name="options">Options used while creating the provider</param>
        private void InitializeProvider(GoFeatureFlagProviderOptions options)
        {
            _exporterMetadata = options.ExporterMetadata ?? new ExporterMetadata();
            _exporterMetadata.Add("provider", ".NET");
            _exporterMetadata.Add("openfeature", true);

            _httpClient = options.HttpMessageHandler != null
                ? new HttpClient(options.HttpMessageHandler)
                : new HttpClient
                {
                    Timeout = options.Timeout.Ticks.Equals(0)
                        ? new TimeSpan(10000 * TimeSpan.TicksPerMillisecond)
                        : options.Timeout
                };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJson));
            _httpClient.BaseAddress = new Uri(options.Endpoint);

            if (options.ApiKey != null)
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.ApiKey);


            var cacheMaxSize = options.CacheMaxSize ?? 10000;
            var cacheMaxTTL = options.CacheMaxTTL ?? TimeSpan.FromSeconds(60);

            var backingMemoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = cacheMaxSize,
                CompactionPercentage = 0.1,                    
            });
            
            _cache = new FusionCache(new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Size = 1,
                    Duration = cacheMaxTTL,
                },
                
            }, backingMemoryCache);
           
        }

        /// <summary>
        ///     Return the metadata associated to this provider.
        /// </summary>
        public override Metadata GetMetadata()
        {
            return new Metadata("GO Feature Flag Provider");
        }

        /// <summary>
        ///     ResolveBooleanValueAsync resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <param name="cancellationToken">Token for cancel the async operation</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        /// <exception cref="TypeMismatchError">If the type of the flag does not match</exception>
        /// <exception cref="FlagNotFoundError">If the flag does not exists</exception>
        /// <exception cref="GeneralError">If an unknown error happen</exception>
        /// <exception cref="FlagDisabled">If the flag is disabled</exception>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
            EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GenerateCacheKey(flagKey, context);
                return await _cache.GetOrSetAsync<ResolutionDetails<bool>>(key, async (_, _) =>
                {
                    var resp = await CallApi(flagKey, defaultValue, context).ConfigureAwait(false);
                    return new ResolutionDetails<bool>(flagKey, bool.Parse(resp.Value.ToString()), ErrorType.None,
                        resp.Reason, resp.Variant, resp.ErrorDetails, resp.Metadata.ToImmutableMetadata());
                }).ConfigureAwait(false);
            }
            catch (FormatException e)
            {
                throw new TypeMismatchError($"flag value {flagKey} had unexpected type", e);
            }
            catch (FlagDisabled)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.None, Reason.Disabled);
            }
        }

        /// <summary>
        ///     ResolveBooleanValueAsync resolve the value for a string Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <param name="cancellationToken">Token for cancel the async operation</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        /// <exception cref="TypeMismatchError">If the type of the flag does not match</exception>
        /// <exception cref="FlagNotFoundError">If the flag does not exists</exception>
        /// <exception cref="GeneralError">If an unknown error happen</exception>
        /// <exception cref="FlagDisabled">If the flag is disabled</exception>
        public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey,
            string defaultValue,
            EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GenerateCacheKey(flagKey, context);
                return await _cache.GetOrSetAsync<ResolutionDetails<string>>(key, async (_,_) =>
                {
                    var resp = await CallApi(flagKey, defaultValue, context).ConfigureAwait(false);
                    if (!(resp.Value is JsonElement element && element.ValueKind == JsonValueKind.String))
                        throw new TypeMismatchError($"flag value {flagKey} had unexpected type");
                    return new ResolutionDetails<string>(flagKey, resp.Value.ToString(), ErrorType.None, resp.Reason,
                        resp.Variant, resp.ErrorDetails, resp.Metadata.ToImmutableMetadata());
                }).ConfigureAwait(false);
            }
            catch (FormatException e)
            {
                throw new TypeMismatchError($"flag value {flagKey} had unexpected type", e);
            }
            catch (FlagDisabled)
            {
                return new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.None, Reason.Disabled);
            }
        }

        /// <summary>
        ///     ResolveBooleanValueAsync resolve the value for an int Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <param name="cancellationToken">Token for cancel the async operation</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        /// <exception cref="TypeMismatchError">If the type of the flag does not match</exception>
        /// <exception cref="FlagNotFoundError">If the flag does not exists</exception>
        /// <exception cref="GeneralError">If an unknown error happen</exception>
        /// <exception cref="FlagDisabled">If the flag is disabled</exception>
        public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
            EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GenerateCacheKey(flagKey, context);
                return await _cache.GetOrSetAsync<ResolutionDetails<int>>(key, async (_,_) =>
                { 
                    var resp = await CallApi(flagKey, defaultValue, context).ConfigureAwait(false);
                    return new ResolutionDetails<int>(flagKey, int.Parse(resp.Value.ToString()), ErrorType.None,
                        resp.Reason, resp.Variant, resp.ErrorDetails, resp.Metadata.ToImmutableMetadata());
                }).ConfigureAwait(false);
            }
            catch (FormatException e)
            {
                throw new TypeMismatchError($"flag value {flagKey} had unexpected type", e);
            }
            catch (FlagDisabled)
            {
                return new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.None, Reason.Disabled);
            }
        }

        /// <summary>
        ///     ResolveBooleanValueAsync resolve the value for a double Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <param name="cancellationToken">Token for cancel the async operation</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        /// <exception cref="TypeMismatchError">If the type of the flag does not match</exception>
        /// <exception cref="FlagNotFoundError">If the flag does not exists</exception>
        /// <exception cref="GeneralError">If an unknown error happen</exception>
        /// <exception cref="FlagDisabled">If the flag is disabled</exception>
        public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey,
            double defaultValue,
            EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GenerateCacheKey(flagKey, context);
                return await _cache.GetOrSetAsync<ResolutionDetails<double>>(key, async (_,_) =>
                {
                    var resp = await CallApi(flagKey, defaultValue, context).ConfigureAwait(false);
                    return new ResolutionDetails<double>(flagKey,
                        double.Parse(resp.Value.ToString(), CultureInfo.InvariantCulture), ErrorType.None,
                        resp.Reason, resp.Variant, resp.ErrorDetails, resp.Metadata.ToImmutableMetadata());
                }).ConfigureAwait(false);
            }
            catch (FormatException e)
            {
                throw new TypeMismatchError($"flag value {flagKey} had unexpected type", e);
            }
            catch (FlagDisabled)
            {
                return new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.None, Reason.Disabled);
            }
        }

        /// <summary>
        ///     ResolveBooleanValueAsync resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <param name="cancellationToken">Token for cancel the async operation</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        /// <exception cref="TypeMismatchError">If the type of the flag does not match</exception>
        /// <exception cref="FlagNotFoundError">If the flag does not exists</exception>
        /// <exception cref="GeneralError">If an unknown error happen</exception>
        /// <exception cref="FlagDisabled">If the flag is disabled</exception>
        public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey,
            Value defaultValue,
            EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GenerateCacheKey(flagKey, context);
                return await _cache.GetOrSetAsync<ResolutionDetails<Value>>(key, async (_,_) =>
                {
                    var resp = await CallApi(flagKey, defaultValue, context).ConfigureAwait(false);
                    if (resp.Value is JsonElement)
                    {
                        var value = ConvertValue((JsonElement)resp.Value);
                        return new ResolutionDetails<Value>(flagKey, value, ErrorType.None, resp.Reason,
                            resp.Variant, resp.ErrorDetails, resp.Metadata.ToImmutableMetadata());
                    }

                    throw new TypeMismatchError($"flag value {flagKey} had unexpected type");
                }).ConfigureAwait(false);
            }
            catch (FormatException e)
            {
                throw new TypeMismatchError($"flag value {flagKey} had unexpected type", e);
            }
            catch (FlagDisabled)
            {
                return new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.None, Reason.Disabled);
            }
        }

        /// <summary>
        ///     This method is handling the call to the GO Feature Flag Relay proxy.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="context">EvaluationContext to convert as parameters for GO Feature Flag Relay Proxy</param>
        /// <typeparam name="T">Type of the data we should retrieve</typeparam>
        /// <returns>The API response in a GoFeatureFlagResponse object.</returns>
        /// <exception cref="FlagNotFoundError">If the flag does not exists</exception>
        /// <exception cref="GeneralError">If an unknown error happen</exception>
        /// <exception cref="FlagDisabled">If the flag is disabled</exception>
        private async Task<OfrepResponse> CallApi<T>(string flagKey, T defaultValue,
            EvaluationContext context = null)
        {
            var request = new OfrepRequest(context);
            var response = await _httpClient.PostAsync($"ofrep/v1/evaluate/flags/{flagKey}",
                new StringContent(request.AsJsonString(), Encoding.UTF8, ApplicationJson)).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new FlagNotFoundError($"flag {flagKey} was not found in your configuration");

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedError("invalid token used to contact GO Feature Flag relay proxy instance");

            if (response.StatusCode >= HttpStatusCode.BadRequest)
                throw new GeneralError("impossible to contact GO Feature Flag relay proxy instance");

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var ofrepResp =
                JsonSerializer.Deserialize<OfrepResponse>(responseBody, options);

            if (Reason.Disabled.Equals(ofrepResp?.Reason))
                throw new FlagDisabled();

            if ("FLAG_NOT_FOUND".Equals(ofrepResp?.ErrorCode))
                throw new FlagNotFoundError($"flag {flagKey} was not found in your configuration");

            if (ofrepResp?.Metadata != null)
                ofrepResp.Metadata = DictionaryConverter.ConvertDictionary(ofrepResp.Metadata);

            return ofrepResp;
        }

        private string GenerateCacheKey(string flagKey, EvaluationContext ctx)
        {
            return ctx != null ? new OfrepRequest(ctx).AsJsonString() : flagKey;
        }

        /// <summary>
        ///     convertValue is converting the object return by the proxy response in the right type.
        /// </summary>
        /// <param name="value">The value we have received</param>
        /// <returns>A converted object</returns>
        /// <exception cref="InvalidCastException">If we are not able to convert the data.</exception>
        private Value ConvertValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;

            if (value.ValueKind == JsonValueKind.False || value.ValueKind == JsonValueKind.True)
                return new Value(value.GetBoolean());

            if (value.ValueKind == JsonValueKind.Number) return new Value(value.GetDouble());

            if (value.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, Value>();
                using var objEnumerator = value.EnumerateObject();
                while (objEnumerator.MoveNext())
                {
                    var current = objEnumerator.Current;
                    var currentValue = ConvertValue(current.Value);
                    if (currentValue != null) dict.Add(current.Name, ConvertValue(current.Value));
                }

                return new Value(new Structure(dict));
            }

            if (value.ValueKind == JsonValueKind.String) return new Value(value.ToString());

            if (value.ValueKind == JsonValueKind.Array)
            {
                using var arrayEnumerator = value.EnumerateArray();
                var arr = new List<Value>();

                while (arrayEnumerator.MoveNext())
                {
                    var current = arrayEnumerator.Current;
                    var convertedValue = ConvertValue(current);
                    if (convertedValue != null) arr.Add(convertedValue);
                }

                return new Value(arr);
            }

            throw new ImpossibleToConvertTypeError($"impossible to convert the object {value}");
        }
    }
}
