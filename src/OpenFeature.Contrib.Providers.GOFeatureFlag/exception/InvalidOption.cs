namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    public class InvalidOption : GoFeatureFlagException
    {
        public InvalidOption(string message) : base(message)
        {
        }
    }
}