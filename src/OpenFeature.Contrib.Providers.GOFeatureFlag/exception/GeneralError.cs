using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public class GeneralError : FeatureProviderException
    {
        public GeneralError(string message, Exception innerException = null) : base(ErrorType.General, message,
            innerException)
        {
        }
    }
}