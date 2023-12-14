namespace OpenFeature.Contrib.Hooks.Otel
{
    internal static class MetricsConstants
    {
        internal const string UNIT = "double";

        internal const string ACTIVE_COUNT_NAME = "feature_flag.evaluation_active_count";
        internal const string REQUESTS_TOTAL_NAME = "feature_flag.evaluation_requests_total";
        internal const string SUCCESS_TOTAL_NAME = "feature_flag.evaluation_success_total";
        internal const string ERROR_TOTAL_NAME = "feature_flag.evaluation_error_total";

        internal const string ACTIVE_DESCRIPTION = "active flag evaluations counter";
        internal const string REQUESTS_DESCRIPTION = "feature flag evaluation request counter";
        internal const string SUCCESS_DESCRIPTION = "feature flag evaluation success counter";
        internal const string ERROR_DESCRIPTION = "feature flag evaluation error counter";

        internal const string KEY_ATTR = "feature_flag.key";
        internal const string PROVIDER_NAME_ATTR = "feature_flag.provider_name";
        internal const string VARIANT_ATTR = "feature_flag.variant";
        internal const string REASON_ATTR = "feature_flag.reason";
        internal const string EXCEPTION_ATTR = "exception";
    }
}
