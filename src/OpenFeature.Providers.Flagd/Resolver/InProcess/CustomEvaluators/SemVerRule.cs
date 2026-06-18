using System;
using System.Text.Json.Nodes;
using Json.Logic;
using Semver;

namespace OpenFeature.Providers.Flagd.Resolver.InProcess.CustomEvaluators;

/// <inheritdoc/>
internal sealed class SemVerRule : IRule
{
    const string OperatorEqual = "=";
    const string OperatorNotEqual = "!=";
    const string OperatorLess = "<";
    const string OperatorLessOrEqual = "<=";
    const string OperatorGreater = ">";
    const string OperatorGreaterOrEqual = ">=";
    const string OperatorMatchMajor = "^";
    const string OperatorMatchMinor = "~";

    // allow v/V prefix and partial versions (e.g. "1", "1.0")
    private const SemVersionStyles LenientStyles =
        SemVersionStyles.AllowV
        | SemVersionStyles.OptionalMinorPatch;

    /// <inheritdoc/>
    public JsonNode Apply(JsonNode args, EvaluationContext context)
    {
        // check if we have at least 3 arguments
        if (args.AsArray().Count < 3)
        {
            return null;
        }

        var rawVersion = Convert.ToString(JsonLogic.Apply(args[0], context));
        var semVerOperator = Convert.ToString(JsonLogic.Apply(args[1], context));
        var rawTargetVersion = Convert.ToString(JsonLogic.Apply(args[2], context));

        var version = NormalizeVersion(rawVersion);
        var targetVersion = NormalizeVersion(rawTargetVersion);

        if (version == null || targetVersion == null)
        {
            return null;
        }

        switch (semVerOperator)
        {
            case OperatorEqual:
                return version.ComparePrecedenceTo(targetVersion) == 0;
            case OperatorNotEqual:
                return version.ComparePrecedenceTo(targetVersion) != 0;
            case OperatorLess:
                return version.ComparePrecedenceTo(targetVersion) < 0;
            case OperatorLessOrEqual:
                return version.ComparePrecedenceTo(targetVersion) <= 0;
            case OperatorGreater:
                return version.ComparePrecedenceTo(targetVersion) > 0;
            case OperatorGreaterOrEqual:
                return version.ComparePrecedenceTo(targetVersion) >= 0;
            case OperatorMatchMajor:
                return version.Major == targetVersion.Major;
            case OperatorMatchMinor:
                return version.Major == targetVersion.Major && version.Minor == targetVersion.Minor;
            default:
                return null;
        }
    }

    /// <summary>
    /// Normalize a version string: coerce numeric inputs to string, allow v/V prefix and partial versions, but reject truly invalid input.
    /// </summary>
    private static SemVersion NormalizeVersion(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        if (SemVersion.TryParse(raw, LenientStyles, out var version))
        {
            return version;
        }

        return null;
    }
}
