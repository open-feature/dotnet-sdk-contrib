using System;

namespace OpenFeature.Contrib.Providers.Flipt
{
    /// <summary>
    /// Flipt provider configuration
    /// </summary>
    public class FliptProviderConfiguration
    {
        /// <summary>
        /// Flipt service address
        /// </summary>
        public Uri ServiceUri { get; set; }

        /// <summary>
        /// Namespace
        /// </summary>
        /// <remarks><see href="https://docs.flipt.io/concepts#namespaces"/></remarks>
        public string Namespace { get; set; }

        /// <summary>
        /// Context key whose value will be used as EntityId
        /// </summary>
        /// <remarks><see href="https://docs.flipt.io/concepts#entities"/></remarks>
        public string TargetingKey { get; set; }

        /// <summary>
        /// Context key whose value will be used as RequestId
        /// </summary>
        public string RequestIdKey { get; set; }

        /// <summary>
        /// Determines whether to use Boolean Evaluation or Variant Evaluation API
        /// </summary>
        /// <remarks><see href="https://docs.flipt.io/concepts#boolean-flags"/></remarks>
        public bool UseBooleanEvaluation { get; set; }
    }
}
