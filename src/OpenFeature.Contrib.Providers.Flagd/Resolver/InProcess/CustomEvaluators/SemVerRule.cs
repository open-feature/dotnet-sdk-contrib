using System.Text.Json.Nodes;
using Json.Logic;
using Semver;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    /// <inheritdoc/>
    internal sealed class SemVerRule : IRule
    {
        internal SemVerRule()
        {
        }

        const string OperatorEqual = "=";
        const string OperatorNotEqual = "!=";
        const string OperatorLess = "<";
        const string OperatorLessOrEqual = "<=";
        const string OperatorGreater = ">";
        const string OperatorGreaterOrEqual = ">=";
        const string OperatorMatchMajor = "^";
        const string OperatorMatchMinor = "~";


        /// <inheritdoc/>
        public JsonNode Apply(JsonNode args, EvaluationContext context)
        {
            // check if we have at least 3 arguments
            if (args.AsArray().Count < 3)
            {
                return false;
            }
            // get the value from the provided evaluation context
            var versionString = JsonLogic.Apply(args[0], context).ToString();

            // get the operator
            var semVerOperator = JsonLogic.Apply(args[1], context).ToString();

            // get the target version
            var targetVersionString = JsonLogic.Apply(args[2], context).ToString();

            //convert to semantic versions
            if (!SemVersion.TryParse(versionString, SemVersionStyles.Strict, out var version) ||
                !SemVersion.TryParse(targetVersionString, SemVersionStyles.Strict, out var targetVersion))
            {
                return false;
            }

            switch (semVerOperator)
            {
                case OperatorEqual:
                    return version.CompareSortOrderTo(targetVersion) == 0;
                case OperatorNotEqual:
                    return version.CompareSortOrderTo(targetVersion) != 0;
                case OperatorLess:
                    return version.CompareSortOrderTo(targetVersion) < 0;
                case OperatorLessOrEqual:
                    return version.CompareSortOrderTo(targetVersion) <= 0;
                case OperatorGreater:
                    return version.CompareSortOrderTo(targetVersion) > 0;
                case OperatorGreaterOrEqual:
                    return version.CompareSortOrderTo(targetVersion) >= 0;
                case OperatorMatchMajor:
                    return version.Major == targetVersion.Major;
                case OperatorMatchMinor:
                    return version.Major == targetVersion.Major && version.Minor == targetVersion.Minor;
                default:
                    return false;
            }
        }
    }
}