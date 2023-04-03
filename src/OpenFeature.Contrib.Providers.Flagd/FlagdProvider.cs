﻿using System;
using System.Linq;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using OpenFeature.Model;
using OpenFeature.Error;

using OpenFeature.Flagd.Grpc;
using Metadata = OpenFeature.Model.Metadata;
using Value = OpenFeature.Model.Value;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;
using System.Net.Sockets;
using System.Net.Http;
using System.Collections.Generic;

namespace OpenFeature.Contrib.Providers.Flagd
{
    /// <summary>
    ///     FlagdProvider is the OpenFeature provider for flagD.
    /// </summary>
    public sealed class FlagdProvider : FeatureProvider
    {
        static int EventStreamRetryBaseBackoff = 1;
        private readonly FlagdConfig _config;
        private readonly Service.ServiceClient _client;
        private readonly Metadata _providerMetadata = new Metadata("flagd Provider");

        private readonly ICache<string, object> _cache;
        private int _eventStreamRetries;
        private int _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;

        private System.Threading.Mutex _mtx;

        /// <summary>
        ///     Constructor of the provider. This constructor uses the value of the following
        ///     environment variables to initialise its client:
        ///     FLAGD_HOST                     - The host name of the flagd server (default="localhost")
        ///     FLAGD_PORT                     - The port of the flagd server (default="8013")
        ///     FLAGD_TLS                      - Determines whether to use https or not (default="false")
        ///     FLAGD_SOCKET_PATH              - Path to the unix socket (default="")
        ///     FLAGD_CACHE                    - Enable or disable the cache (default="false")
        ///     FLAGD_MAX_CACHE_SIZE           - The maximum size of the cache (default="10")
        ///     FLAGD_MAX_EVENT_STREAM_RETRIES - The maximum amount of retries for establishing the EventStream
        /// </summary>
        public FlagdProvider()
        {
            _config = new FlagdConfig();

            _client = buildClientForPlatform(_config.GetUri());

            _mtx = new System.Threading.Mutex();

            if (_config.CacheEnabled)
            {
                _cache = new LRUCache<string, object>(_config.MaxCacheSize);
                Task.Run(async () =>
                {
                    await HandleEvents();
                });
            }
        }

        /// <summary>
        ///     Constructor of the provider.
        ///     <param name="url">The URL of the flagD server</param>
        ///     <exception cref="ArgumentNullException">if no url is provided.</exception>
        /// </summary>
        public FlagdProvider(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            _mtx = new System.Threading.Mutex();

            _client = buildClientForPlatform(url);
        }


        // just for testing, internal but visible in tests
        internal FlagdProvider(Service.ServiceClient client, FlagdConfig config, ICache<string, object> cache = null)
        {
            _mtx = new System.Threading.Mutex();
            _client = client;
            _config = config;
            _cache = cache;

            if (_config.CacheEnabled)
            {
                Task.Run(async () =>
                {
                    await HandleEvents();
                });
            }
        }

        /// <summary>
        /// Get the provider name.
        /// </summary>
        public static string GetProviderName()
        {
            return Api.Instance.GetProviderMetadata().Name;
        }

        /// <summary>
        ///     Return the metadata associated to this provider.
        /// </summary>
        public override Metadata GetMetadata() => _providerMetadata;

        /// <summary>
        ///     Return the Grpc client of the provider
        /// </summary>
        public Service.ServiceClient GetClient() => _client;

        /// <summary>
        ///     ResolveBooleanValue resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return await ResolveValue(flagKey, async contextStruct =>
            {
                var resolveBooleanResponse = await _client.ResolveBooleanAsync(new ResolveBooleanRequest
                {
                    Context = contextStruct,
                    FlagKey = flagKey
                });

                return new ResolutionDetails<bool>(
                    flagKey: flagKey,
                    value: (bool)resolveBooleanResponse.Value,
                    reason: resolveBooleanResponse.Reason,
                    variant: resolveBooleanResponse.Variant
                );
            }, context);
        }

        /// <summary>
        ///     ResolveStringValue resolve the value for a string Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            return await ResolveValue(flagKey, async contextStruct =>
            {
                var resolveStringResponse = await _client.ResolveStringAsync(new ResolveStringRequest
                {
                    Context = contextStruct,
                    FlagKey = flagKey
                });

                return new ResolutionDetails<string>(
                    flagKey: flagKey,
                    value: resolveStringResponse.Value,
                    reason: resolveStringResponse.Reason,
                    variant: resolveStringResponse.Variant
                );
            }, context);
        }

        /// <summary>
        ///     ResolveIntegerValue resolve the value for an int Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            return await ResolveValue(flagKey, async contextStruct =>
            {
                var resolveIntResponse = await _client.ResolveIntAsync(new ResolveIntRequest
                {
                    Context = contextStruct,
                    FlagKey = flagKey
                });

                return new ResolutionDetails<int>(
                    flagKey: flagKey,
                    value: (int)resolveIntResponse.Value,
                    reason: resolveIntResponse.Reason,
                    variant: resolveIntResponse.Variant
                );
            }, context);
        }

        /// <summary>
        ///     ResolveDoubleValue resolve the value for a double Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            return await ResolveValue(flagKey, async contextStruct =>
            {
                var resolveDoubleResponse = await _client.ResolveFloatAsync(new ResolveFloatRequest
                {
                    Context = contextStruct,
                    FlagKey = flagKey
                });

                return new ResolutionDetails<double>(
                    flagKey: flagKey,
                    value: resolveDoubleResponse.Value,
                    reason: resolveDoubleResponse.Reason,
                    variant: resolveDoubleResponse.Variant
                );
            }, context);
        }

        /// <summary>
        ///     ResolveStructureValue resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            return await ResolveValue(flagKey, async contextStruct =>
            {
                var resolveObjectResponse = await _client.ResolveObjectAsync(new ResolveObjectRequest
                {
                    Context = contextStruct,
                    FlagKey = flagKey
                });

                return new ResolutionDetails<Value>(
                    flagKey: flagKey,
                    value: ConvertObjectToValue(resolveObjectResponse.Value),
                    reason: resolveObjectResponse.Reason,
                    variant: resolveObjectResponse.Variant
                );
            }, context);
        }

        private async Task<ResolutionDetails<T>> ResolveValue<T>(string flagKey, Func<Struct, Task<ResolutionDetails<T>>> resolveDelegate, EvaluationContext context = null)
        {
            try
            {
                if (_config.CacheEnabled)
                {
                    var value = _cache.TryGet(flagKey);

                    if (value != null)
                    {
                        return (ResolutionDetails<T>)value;
                    }
                }
                var result = await resolveDelegate.Invoke(ConvertToContext(context));

                if (result.Reason.Equals("STATIC") && _config.CacheEnabled)
                {
                    _cache.Add(flagKey, result);
                }

                return result;
            }
            catch (RpcException e)
            {
                throw GetOFException(e);
            }
        }

        /// <summary>
        ///     GetOFException returns a OpenFeature Exception containing an error code to describe the encountered error.
        /// </summary>
        /// <param name="e">The exception thrown by the Grpc client</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        private FeatureProviderException GetOFException(Grpc.Core.RpcException e)
        {
            switch (e.Status.StatusCode)
            {
                case Grpc.Core.StatusCode.NotFound:
                    return new FeatureProviderException(Constant.ErrorType.FlagNotFound, e.Status.Detail, e);
                case Grpc.Core.StatusCode.Unavailable:
                    return new FeatureProviderException(Constant.ErrorType.ProviderNotReady, e.Status.Detail, e);
                case Grpc.Core.StatusCode.InvalidArgument:
                    return new FeatureProviderException(Constant.ErrorType.TypeMismatch, e.Status.Detail, e);
                default:
                    return new FeatureProviderException(Constant.ErrorType.General, e.Status.Detail, e);
            }
        }

        private async Task HandleEvents()
        {
            while (_eventStreamRetries < _config.MaxEventStreamRetries)
            {
                var call = _client.EventStream(new Empty());
                try
                {
                    // Read the response stream asynchronously
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;

                        switch (response.Type.ToLower())
                        {
                            case "configuration_change":
                                HandleConfigurationChangeEvent(response.Data);
                                break;
                            case "provider_ready":
                                HandleProviderReadyEvent();
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    // Handle the dropped connection by reconnecting and retrying the stream
                    await HandleErrorEvent();
                }
            }
        }

        private void HandleConfigurationChangeEvent(Struct data)
        {
            // if we don't have a cache, we don't need to remove anything
            if (!_config.CacheEnabled || !data.Fields.ContainsKey("flags"))
            {
                return;
            }

            try
            {
                if (data.Fields.TryGetValue("flags", out ProtoValue val))
                {
                    if (val.KindCase == ProtoValue.KindOneofCase.StructValue)
                    {
                        val.StructValue.Fields.ToList().ForEach(flag =>
                        {
                            _cache.Delete(flag.Key);
                        });
                    }
                    var structVal = val.StructValue;
                }
            }
            catch (Exception)
            {
                // purge the cache if we could not handle the configuration change event
                _cache.Purge();
            }

        }

        private void HandleProviderReadyEvent()
        {
            _mtx.WaitOne();
            _eventStreamRetries = 0;
            _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;
            _mtx.ReleaseMutex();
            _cache.Purge();
        }

        private async Task HandleErrorEvent()
        {
            _mtx.WaitOne();
            _eventStreamRetries++;

            if (_eventStreamRetries > _config.MaxEventStreamRetries)
            {
                return;
            }
            _eventStreamRetryBackoff = _eventStreamRetryBackoff * 2;
            _mtx.ReleaseMutex();
            await Task.Delay(_eventStreamRetryBackoff * 1000);
        }

        /// <summary>
        ///     ConvertToContext converts the given EvaluationContext to a Struct.
        /// </summary>
        /// <param name="ctx">The evaluation context</param>
        /// <returns>A Struct object containing the evaluation context</returns>
        private static Struct ConvertToContext(EvaluationContext ctx)
        {
            if (ctx == null)
            {
                return new Struct();
            }

            var values = new Struct();
            foreach (var entry in ctx)
            {
                values.Fields.Add(entry.Key, ConvertToProtoValue(entry.Value));
            }

            return values;
        }

        /// <summary>   
        ///     ConvertToProtoValue converts the given Value to a ProtoValue.
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>A ProtoValue object representing the given value</returns>
        private static ProtoValue ConvertToProtoValue(Value value)
        {
            if (value.IsList)
            {
                return ProtoValue.ForList(value.AsList.Select(ConvertToProtoValue).ToArray());
            }

            if (value.IsStructure)
            {
                var values = new Struct();

                foreach (var entry in value.AsStructure)
                {
                    values.Fields.Add(entry.Key, ConvertToProtoValue(entry.Value));
                }

                return ProtoValue.ForStruct(values);
            }

            if (value.IsBoolean)
            {
                return ProtoValue.ForBool(value.AsBoolean ?? false);
            }

            if (value.IsString)
            {
                return ProtoValue.ForString(value.AsString);
            }

            if (value.IsNumber)
            {
                return ProtoValue.ForNumber(value.AsDouble ?? 0.0);
            }

            return ProtoValue.ForNull();
        }

        /// <summary>   
        ///     ConvertObjectToValue converts the given Struct to a Value.
        /// </summary>
        /// <param name="src">The struct</param>
        /// <returns>A Value object representing the given struct</returns>
        private static Value ConvertObjectToValue(Struct src) =>
            new Value(new Structure(src.Fields
                .ToDictionary(entry => entry.Key, entry => ConvertToValue(entry.Value))));

        /// <summary>   
        ///     ConvertToValue converts the given ProtoValue to a Value.
        /// </summary>
        /// <param name="src">The value, represented as ProtoValue</param>
        /// <returns>A Value object representing the given value</returns>
        private static Value ConvertToValue(ProtoValue src)
        {
            switch (src.KindCase)
            {
                case ProtoValue.KindOneofCase.ListValue:
                    return new Value(src.ListValue.Values.Select(ConvertToValue).ToList());
                case ProtoValue.KindOneofCase.StructValue:
                    return new Value(ConvertObjectToValue(src.StructValue));
                case ProtoValue.KindOneofCase.None:
                case ProtoValue.KindOneofCase.NullValue:
                case ProtoValue.KindOneofCase.NumberValue:
                case ProtoValue.KindOneofCase.StringValue:
                case ProtoValue.KindOneofCase.BoolValue:
                default:
                    return ConvertToPrimitiveValue(src);
            }
        }

        /// <summary>   
        ///     ConvertToPrimitiveValue converts the given ProtoValue to a Value.
        /// </summary>
        /// <param name="value">The value, represented as ProtoValue</param>
        /// <returns>A Value object representing the given value as a primitive data type</returns>
        private static Value ConvertToPrimitiveValue(ProtoValue value)
        {
            switch (value.KindCase)
            {
                case ProtoValue.KindOneofCase.BoolValue:
                    return new Value(value.BoolValue);
                case ProtoValue.KindOneofCase.StringValue:
                    return new Value(value.StringValue);
                case ProtoValue.KindOneofCase.NumberValue:
                    return new Value(value.NumberValue);
                case ProtoValue.KindOneofCase.NullValue:
                case ProtoValue.KindOneofCase.StructValue:
                case ProtoValue.KindOneofCase.ListValue:
                case ProtoValue.KindOneofCase.None:
                default:
                    return new Value();
            }
        }

        private static Service.ServiceClient buildClientForPlatform(Uri url)
        {
            var useUnixSocket = url.ToString().StartsWith("unix://");

            if (!useUnixSocket)
            {
#if NET462
                 return new Service.ServiceClient(GrpcChannel.ForAddress(url, new GrpcChannelOptions
                {
                    HttpHandler = new WinHttpHandler(),
                }));
#else
                return new Service.ServiceClient(GrpcChannel.ForAddress(url));
#endif
            }

#if NET5_0_OR_GREATER
            var udsEndPoint = new UnixDomainSocketEndPoint(url.ToString().Substring("unix://".Length));
            var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
            var socketsHttpHandler = new SocketsHttpHandler
            {
                ConnectCallback = connectionFactory.ConnectAsync
            };
            
            // point to localhost and let the custom ConnectCallback handle the communication over the unix socket
            // see https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-uds?view=aspnetcore-7.0 for more details
            return new Service.ServiceClient(GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpHandler = socketsHttpHandler,
            }));
#endif
            // unix socket support is not available in this dotnet version
            throw new Exception("unix sockets are not supported in this version.");
        }
    }
}

