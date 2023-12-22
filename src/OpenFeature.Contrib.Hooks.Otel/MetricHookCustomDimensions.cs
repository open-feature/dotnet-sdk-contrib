using System.Collections.Generic;

namespace OpenFeature.Contrib.Hooks.Otel
{
    /// <summary>
    /// Represents a custom dimension list for a metric hook.
    /// </summary>
    public class MetricHookCustomDimensions
    {
        private readonly List<KeyValuePair<string, object>> _keyValuePairs = new List<KeyValuePair<string, object>>();

        /// <summary>
        /// Adds a custom dimension to the list.
        /// </summary>
        /// <param name="key">The key of the custom dimension.</param>
        /// <param name="value">The value of the custom dimension.</param>
        /// <returns>The custom dimension list.</returns>
        public MetricHookCustomDimensions Add(string key, object value)
        {
            _keyValuePairs.Add(new KeyValuePair<string, object>(key, value));
            return this;
        }

        internal KeyValuePair<string, object>[] GetTagList()
        {
            return _keyValuePairs.ToArray();
        }
    }
}