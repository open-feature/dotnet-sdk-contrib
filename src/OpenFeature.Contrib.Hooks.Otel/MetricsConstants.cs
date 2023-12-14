namespace OpenFeature.Contrib.Hooks.Otel
{
    internal static class MetricsConstants
    {
        internal const string Unit = "double";

        internal const string ActiveCountName = "feature_flag.evaluation_active_count";
        internal const string RequestsTotalName = "feature_flag.evaluation_requests_total";
        internal const string SuccessTotalName = "feature_flag.evaluation_success_total";
        internal const string ErrorTotalName = "feature_flag.evaluation_error_total";

        internal const string ActiveDescription = "active flag evaluations counter";
        internal const string RequestsDescription = "feature flag evaluation request counter";
        internal const string SuccessDescription = "feature flag evaluation success counter";
        internal const string ErrorDescription = "feature flag evaluation error counter";

        internal const string KeyAttr = "feature_flag.key";
        internal const string ProviderNameAttr = "feature_flag.provider_name";
        internal const string VariantAttr = "feature_flag.variant";
        internal const string ReasonAttr = "feature_flag.reason";
        internal const string ExceptionAttr = "exception";
    }
}
