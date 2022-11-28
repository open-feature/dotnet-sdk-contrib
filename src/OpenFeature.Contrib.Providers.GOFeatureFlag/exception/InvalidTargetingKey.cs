using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public class InvalidTargetingKey : FeatureProviderException
    {
        public InvalidTargetingKey(string message, Exception innerException = null) : base(ErrorType.InvalidContext,
            message, innerException)
        {
        }
    }
}