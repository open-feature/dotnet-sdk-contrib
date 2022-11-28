using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public class TypeMismatchError : FeatureProviderException
    {
        public TypeMismatchError(string message, Exception innerException = null) : base(ErrorType.TypeMismatch,
            message, innerException)
        {
        }
    }
}