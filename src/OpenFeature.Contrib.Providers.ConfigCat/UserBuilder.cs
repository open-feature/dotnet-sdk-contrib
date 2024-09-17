using System;
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

            var user = new User(context.GetUserId());

            foreach (var value in context)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals("EMAIL", value.Key))
                {
                    user.Email = value.Value.AsString;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals("COUNTRY", value.Key))
                {
                    user.Country = value.Value.AsString;
                }
                else
                {
                    user.Custom.Add(value.Key, value.Value.AsString);
                }
            }

            return user;
        }

        private static string GetUserId(this EvaluationContext context)
        {
            var pair = context.AsDictionary().FirstOrDefault(x => PossibleUserIds.Contains(x.Key, StringComparer.OrdinalIgnoreCase));

            return pair.Key != null ? pair.Value.AsString : "<n/a>";
        }
    }
}