using Flipt.Evaluation;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using System.Diagnostics;

namespace OpenFeature.Contrib.Providers.Flipt
{
    internal static class FliptConverter
    {
        /// <summary>
        /// Converts an EvaluationReason value into a corresponding string representation based on predefined reasons.
        /// </summary>
        /// <param name="reason">Predefined OpenFeature reason.</param>
        /// <returns></returns>
        public static string ConvertReason(EvaluationReason reason)
        {
            switch (reason)
            {
                case EvaluationReason.UnknownEvaluationReason:
                    return Reason.Unknown;
                case EvaluationReason.FlagDisabledEvaluationReason:
                    return Reason.Disabled;
                case EvaluationReason.MatchEvaluationReason:
                    return Reason.TargetingMatch;
                case EvaluationReason.DefaultEvaluationReason:
                    return Reason.Default;
                default:
                    return Reason.Default;
            }
        }

        /// <summary>
        /// Creates Flipt evaluation request.
        /// </summary>
        /// <param name="flagKey">Flag key.</param>
        /// <param name="context">Evaluation context.</param>
        /// <param name="config">Provider configuration.</param>
        /// <returns>Flipt evaluation request.</returns>
        /// <exception cref="InvalidContextException">Unable to convert context value.</exception>
        public static EvaluationRequest CreateRequest(string flagKey, EvaluationContext context, FliptProviderConfiguration config)
        {
            var request = new EvaluationRequest
            {
                NamespaceKey = config.Namespace,
                FlagKey = flagKey
            };

            if (Activity.Current != null)
            {
                request.RequestId = Activity.Current.Id;
            }

            if (context == null || context.Count == 0)
            {
                return request;
            }

            foreach (var item in context)
            {
                var key = item.Key;
                var value = item.Value;

                if (value.IsNull || value.IsList || value.IsStructure)
                {
                    // Skip null, lists and complex objects
                    continue;
                }

                if (key == config.TargetingKey && value.IsString)
                {
                    // Skip targeting key and add its value as EntityId to request
                    request.EntityId = value.AsString;
                    continue;
                }

                if (key == config.RequestIdKey && value.IsString)
                {
                    // Skip request id key and add its value as RequestId to request
                    request.RequestId = value.AsString;
                    continue;
                }

                if (value.IsString)
                {
                    request.Context.Add(key, value.AsString);
                }
                else if (value.IsBoolean)
                {
                    request.Context.Add(key, value.AsBoolean.ToString());
                }
                else if (value.IsNumber)
                {
                    request.Context.Add(key, value.AsDouble.ToString());
                }
                else if (value.IsDateTime)
                {
                    request.Context.Add(key, $"{value.AsDateTime.Value:o}");
                }
                else
                {
                    throw new InvalidContextException($"Unable to convert context value with key: {key}.");
                }
            }

            return request;
        }
    }
}
