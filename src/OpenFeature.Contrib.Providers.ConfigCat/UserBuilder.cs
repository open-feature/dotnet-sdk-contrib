using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client;
using OpenFeature.Model;

namespace OpenFeature.Contrib.ConfigCat
{
    internal static class UserBuilder
    {
        private static readonly string[] PossibleUserIds = { "ID", "IDENTIFIER" };

        internal static User BuildUser(this EvaluationContext context)
        {
            if (context == null)
            {
                return null;
            }

            var user = context.TryGetValuesInsensitive(PossibleUserIds, out var pair)
                ? new User(pair.Value.AsString)
                : new User(Guid.NewGuid().ToString());

            foreach (var value in context)
            {
                switch (value.Key.ToUpperInvariant())
                {
                    case "EMAIL":
                        user.Email = value.Value.AsString;
                        continue;
                    case "COUNTRY":
                        user.Country = value.Value.AsString;
                        continue;
                    default:
                        user.Custom.Add(value.Key, value.Value.AsString);
                        continue;
                }
            }

            return user;
        }

        private static bool TryGetValuesInsensitive(this EvaluationContext context, string[] keys,
            out KeyValuePair<string, Value> pair)
        {
            pair = context.AsDictionary().FirstOrDefault(x => keys.Contains(x.Key.ToUpperInvariant()));

            return pair.Key != null;
        }
    }
}