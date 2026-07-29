using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client;
using OpenFeature.Model;

namespace OpenFeature.Contrib.ConfigCat;

internal static class UserBuilder
{
    private static readonly string[] PossibleUserIds = { "ID", "IDENTIFIER" };

    internal static User? BuildUser(this EvaluationContext? context)
    {
        if (context is null)
        {
            return null;
        }

        string? identifier = context.TargetingKey, email = null, country = null;
        Dictionary<string, object>? customAttributes = null;

        foreach (var entry in context)
        {
            // NOTE: Attribute key matching shouldn't really be case-insensitive as it may lead to confusing behavior
            // in some edge cases. However, fixing this would be a significant breaking change, so we keep it this way.

            if (identifier is null && PossibleUserIds.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                identifier = entry.Value.AsString;
            }
            else if (email is null && StringComparer.OrdinalIgnoreCase.Equals("EMAIL", entry.Key))
            {
                email = entry.Value.AsString;
            }
            else if (country is null && StringComparer.OrdinalIgnoreCase.Equals("COUNTRY", entry.Key))
            {
                country = entry.Value.AsString;
            }

            if (!entry.Value.IsNull && entry.Key is not (nameof(User.Identifier) or nameof(User.Email) or nameof(User.Country)))
            {
                // NOTE: No need to check for unsupported attribute values as the ConfigCat SDK handles those internally.
                (customAttributes ??= new())[entry.Key] = entry.Value.AsObject!;
            }
        }

        var user = new User(identifier ?? "<n/a>")
        {
            Email = email,
            Country = country,
            Custom = customAttributes!
        };

        return user;
    }
}
