using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public abstract class GoFeatureFlagException : Exception
    {
        public GoFeatureFlagException()
        {
        }

        public GoFeatureFlagException(string message)
            : base(message)
        {
        }

        public GoFeatureFlagException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}