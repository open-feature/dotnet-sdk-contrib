namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    public class GOFeatureFlagRequest<T>
    {
        public GoFeatureFlagUser User { get; set; }
        public T DefaultValue { get; set; }
    }
}