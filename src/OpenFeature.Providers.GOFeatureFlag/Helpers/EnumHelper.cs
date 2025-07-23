using System;
using System.ComponentModel;

namespace OpenFeature.Providers.GOFeatureFlag.Helpers;

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
    public static T GetEnumValueFromDescription<T>(string? description) where T : Enum
    {
        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentException("Description cannot be null or empty", nameof(description));
        }
        foreach (var field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr && attr.Description == description)
            {
                if (field.GetValue(null) is T value)
                {
                    return value;
                }
            }

            if (field.Name == description)
            {
                if (field.GetValue(null) is T value)
                {
                    return value;
                }
            }
        }

        throw new ArgumentException($"Not found: {description}", nameof(description));
    }
}
