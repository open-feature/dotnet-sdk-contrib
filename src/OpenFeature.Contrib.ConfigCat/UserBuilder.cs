using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client;
using OpenFeature.Model;

namespace OpenFeature.Contrib.ConfigCat
{
    internal static class UserBuilder
    {
        internal static User BuildUser(this EvaluationContext context)
        {
            if(context == null)
            {
                return null;
            }

            var user = new User(Guid.NewGuid().ToString());

            if(context.TryGetValueInsensitive("id", out var pair))
            {
                user = new User(pair.Value.AsString);
            }

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

        private static bool TryGetValueInsensitive(this EvaluationContext context, string key,
            out KeyValuePair<string, Value> pair)
        {
            pair = context.AsDictionary().FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            return pair.Key != null;
        }
    }
}