using System;
using System.Collections.Generic;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Schematic
{
    internal static class ValueExtensions
    {
        public static T ToObject<T>(this Value value) where T : class
        {
            if (value == null || value.IsNull) return null;

            if (value.IsStructure)
            {
                var structure = value.AsStructure;
                if (structure == null) return null;

                if (typeof(T) == typeof(Dictionary<string, string>))
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var kvp in structure)
                    {
                        var stringValue = kvp.Value?.AsString;
                        if (stringValue != null)
                        {
                            dict[kvp.Key] = stringValue;
                        }
                    }
                    return dict as T;
                }
                else if (typeof(T) == typeof(Dictionary<string, object>))
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var kvp in structure)
                    {
                        var objValue = kvp.Value?.AsObject;
                        if (objValue != null)
                        {
                            dict[kvp.Key] = objValue;
                        }
                    }
                    return dict as T;
                }
            }
            return null;
        }
    }
}
