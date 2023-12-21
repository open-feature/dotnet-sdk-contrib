using System.Diagnostics;

namespace OpenFeature.Contrib.Hooks.Otel
{
    /// <summary>
    /// Represents a custom dimension list for a metric hook.
    /// </summary>
    public class MetricHookCustomDimensions
    {
        private readonly TagList _keyValuePairs = new TagList();

        /// <summary>
        /// Adds a custom dimension to the list.
        /// </summary>
        /// <param name="key">The key of the custom dimension.</param>
        /// <param name="value">The value of the custom dimension.</param>
        /// <returns>The custom dimension list.</returns>
        public MetricHookCustomDimensions Add(string key, string value)
        {
            _keyValuePairs.Add(key, value);
            return this;
        }

        internal TagList GetTagList()
        {
            return _keyValuePairs;
        }
    }
}