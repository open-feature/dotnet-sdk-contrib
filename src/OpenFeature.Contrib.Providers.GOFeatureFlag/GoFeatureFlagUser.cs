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
        private string Key { get; set; }
        private bool Anonymous { get; set; }
        private Dictionary<string, object> Custom { get; set; }

        /**
         * FromEvaluationContext convert the evaluation context into a GOFeatureFlagUser Object.
         */
        public static GoFeatureFlagUser FromEvaluationContext(EvaluationContext ctx)
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