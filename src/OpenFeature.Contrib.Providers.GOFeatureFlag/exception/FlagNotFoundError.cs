using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public class FlagNotFoundError : FeatureProviderException
    {
        public FlagNotFoundError(string message, Exception innerException = null) : base(ErrorType.FlagNotFound,
            message, innerException)
        {
        }
    }
}