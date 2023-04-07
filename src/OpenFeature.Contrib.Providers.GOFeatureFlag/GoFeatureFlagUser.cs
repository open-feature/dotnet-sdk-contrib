using System.Collections.Generic;
using System.Linq;
using OpenFeature.Contrib.Providers.GOFeatureFlag.exception;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    /// <summary>
    ///     GOFeatureFlagUser is the representation of a User inside GO Feature Flag.
    /// </summary>
    public class GoFeatureFlagUser
    {
        private const string AnonymousField = "anonymous";
        private const string KeyField = "targetingKey";

        /// <summary>
        ///     The targeting key for the user.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        ///     Is the user Anonymous.
        /// </summary>
        public bool Anonymous { get; private set; }

        /// <summary>
        ///     Additional Custom Data to pass to GO Feature Flag.
        /// </summary>
        public Dictionary<string, object> Custom { get; private set; }

        /**
         * Convert the evaluation context into a GOFeatureFlagUser Object.
         */
        public static implicit operator GoFeatureFlagUser(EvaluationContext ctx)
        {
            try
            {
                if (ctx is null)
                    throw new InvalidEvaluationContext("GO Feature Flag need an Evaluation context to work.");
                if (!ctx.GetValue(KeyField).IsString)
                    throw new InvalidTargetingKey("targetingKey field MUST be a string.");
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidTargetingKey("targetingKey field is mandatory.", e);
            }

            var anonymous = ctx.ContainsKey(AnonymousField) && ctx.GetValue(AnonymousField).IsBoolean
                ? ctx.GetValue(AnonymousField).AsBoolean
                : false;

            var custom = ctx.AsDictionary().ToDictionary(x => x.Key, x => x.Value.AsObject);
            custom.Remove(AnonymousField);
            custom.Remove(KeyField);

            return new GoFeatureFlagUser
            {
                Key = ctx.GetValue("targetingKey").AsString,
                Anonymous = anonymous.Value,
                Custom = custom
            };
        }
    }
}