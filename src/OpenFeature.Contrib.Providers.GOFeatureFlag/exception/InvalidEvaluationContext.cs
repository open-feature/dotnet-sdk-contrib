using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public class InvalidEvaluationContext : FeatureProviderException
    {
        public InvalidEvaluationContext(string message, Exception innerException = null) : base(
            ErrorType.InvalidContext, message, innerException)
        {
        }
    }
}