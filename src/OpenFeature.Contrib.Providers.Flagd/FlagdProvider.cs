﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using OpenFeature.Model;

using Schema.V1;
using Value = OpenFeature.Model.Value;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;

namespace OpenFeature.Contrib.Providers.Flagd
{
    /// <summary>
    ///     FlagdProvider is the OpenFeature provider for flagD.
    /// </summary>
    public sealed class FlagdProvider : FeatureProvider
    {
        private readonly Service.ServiceClient _client;
        private readonly Metadata _providerMetadata = new Metadata("flagD Provider");

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

            _client = new Service.ServiceClient(GrpcChannel.ForAddress(url));
        }

        /// <summary>
        ///     Constructor of the provider.
        ///     <param name="client">The Grpc client used to communicate with the server</param>
        ///     <exception cref="ArgumentNullException">if no url is provided.</exception>
        /// </summary>
        public FlagdProvider(Service.ServiceClient client)
        {
            _client = client;
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
        ///     ResolveBooleanValue resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            try
            {
                var resolveBooleanResponse = await _client.ResolveBooleanAsync(new ResolveBooleanRequest
                {
                    Context = ConvertToContext(context),
                    FlagKey = flagKey
                });

                return new ResolutionDetails<bool>(
                    flagKey: flagKey,
                    value: resolveBooleanResponse.Value,
                    reason: resolveBooleanResponse.Reason,
                    variant: resolveBooleanResponse.Variant
                );
            }
            catch (Grpc.Core.RpcException e)
            {
                return GetDefaultWithException<bool>(e, flagKey, defaultValue);
            }
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
            try
            {
                var resolveBooleanResponse = await _client.ResolveStringAsync(new ResolveStringRequest
                {
                    Context = ConvertToContext(context),
                    FlagKey = flagKey
                });

                return new ResolutionDetails<string>(
                    flagKey: flagKey,
                    value: resolveBooleanResponse.Value,
                    reason: resolveBooleanResponse.Reason,
                    variant: resolveBooleanResponse.Variant
                );
            }
            catch (Grpc.Core.RpcException e)
            {
                return GetDefaultWithException<string>(e, flagKey, defaultValue);
            }

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
            try
            {
                var resolveIntResponse = await _client.ResolveIntAsync(new ResolveIntRequest
                {
                    Context = ConvertToContext(context),
                    FlagKey = flagKey
                });

                return new ResolutionDetails<int>(
                    flagKey: flagKey,
                    value: (int)resolveIntResponse.Value,
                    reason: resolveIntResponse.Reason,
                    variant: resolveIntResponse.Variant
                );
            }
            catch (Grpc.Core.RpcException e)
            {
                return GetDefaultWithException<int>(e, flagKey, defaultValue);
            }
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
            try
            {
                var resolveDoubleResponse = await _client.ResolveFloatAsync(new ResolveFloatRequest
                {
                    Context = ConvertToContext(context),
                    FlagKey = flagKey
                });

                return new ResolutionDetails<double>(
                    flagKey: flagKey,
                    value: resolveDoubleResponse.Value,
                    reason: resolveDoubleResponse.Reason,
                    variant: resolveDoubleResponse.Variant
                );
            }
            catch (Grpc.Core.RpcException e)
            {
                return GetDefaultWithException<double>(e, flagKey, defaultValue);
            }
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
            try
            {
                var resolveObjectResponse = await _client.ResolveObjectAsync(new ResolveObjectRequest
                {
                    Context = ConvertToContext(context),
                    FlagKey = flagKey
                });

                return new ResolutionDetails<Value>(
                    flagKey: flagKey,
                    value: ConvertObjectToValue(resolveObjectResponse.Value),
                    reason: resolveObjectResponse.Reason,
                    variant: resolveObjectResponse.Variant
                );
            }
            catch (Grpc.Core.RpcException e)
            {
                return GetDefaultWithException<Value>(e, flagKey, defaultValue);
            }
        }

        /// <summary>
        ///     GetDefaultWithException returns the default value for a flag, together with some error information about why thy flag could not be retrieved by the provider.
        /// </summary>
        /// <param name="e">The exception thrown by the Grpc client</param>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value to return</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        private ResolutionDetails<T> GetDefaultWithException<T>(Grpc.Core.RpcException e, String flagKey, T defaultValue)
        {
            if (e.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return new ResolutionDetails<T>(
                    flagKey: flagKey,
                    value: defaultValue,
                    reason: Constant.Reason.Error,
                    errorType: Constant.ErrorType.FlagNotFound,
                    errorMessage: e.Status.Detail.ToString()
                );
            }
            else if (e.Status.StatusCode == Grpc.Core.StatusCode.Unavailable)
            {
                return new ResolutionDetails<T>(
                   flagKey: flagKey,
                   value: defaultValue,
                   reason: Constant.Reason.Error,
                   errorType: Constant.ErrorType.ProviderNotReady,
                   errorMessage: e.Status.Detail.ToString()
               );
            }
            else if (e.Status.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
            {
                return new ResolutionDetails<T>(
                    flagKey: flagKey,
                    value: defaultValue,
                    reason: Constant.Reason.Error,
                    errorType: Constant.ErrorType.TypeMismatch,
                    errorMessage: e.Status.Detail.ToString()
                );
            }
            return new ResolutionDetails<T>(
                flagKey: flagKey,
                value: defaultValue,
                reason: Constant.Reason.Error,
                errorType: Constant.ErrorType.General,
                errorMessage: e.Status.Detail.ToString()
            );
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
    }
}
