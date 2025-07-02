using System;
using System.ComponentModel;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.helper;

/// <summary>
///     EnumHelper is providing helper method to work with enums
/// </summary>
public static class EnumHelper
{
    /// <summary>
    ///     Return an enum item from the description
    /// </summary>
    /// <param name="description"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T GetEnumValueFromDescription<T>(string description) where T : Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (attr != null && attr.Description == description)
            {
                return (T)field.GetValue(null);
            }

            if (field.Name == description)
            {
                return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException($"Not found: {description}", nameof(description));
    }
}
