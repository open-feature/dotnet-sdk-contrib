using System;
using System.Linq;
using System.Net.Http;
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
    /// A stub class.
    /// </summary>
    public sealed class FlagdProvider : FeatureProvider
    {
        private readonly Service.ServiceClient _client;
        private readonly Metadata _providerMetadata = new Metadata("flagD Provider");

        public FlagdProvider(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            
#if NETSTANDARD2_0
            _client = new Service.ServiceClient(GrpcChannel.ForAddress(url));
#else
            _client = new Service.ServiceClient(GrpcChannel.ForAddress(url, new GrpcChannelOptions
            {
                HttpHandler = new WinHttpHandler()
            }));
#endif
        }
        
        /// <summary>
        /// Get the provider name.
        /// </summary>
        public static string GetProviderName()
        {
            return Api.Instance.GetProviderMetadata().Name;
        }
        
        public override Metadata GetMetadata() => _providerMetadata;

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
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

        public override async Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
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

        public override async Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
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

        public override async Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
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

        public override async Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
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

        private static Value ConvertObjectToValue(Struct src) =>
            new Value(new Structure(src.Fields
                .ToDictionary(entry => entry.Key, entry => ConvertToValue(entry.Value))));

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


