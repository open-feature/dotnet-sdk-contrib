using System;
using JsonLogic.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Semver;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    /// <inheritdoc/>
    public class SemVerEvaluator
    {
        internal ILogger Logger { get; set; }

        internal SemVerEvaluator()
        {
            var loggerFactory = LoggerFactory.Create(
                builder => builder
                    // add console as logging target
                    .AddConsole()
                    // add debug output as logging target
                    .AddDebug()
                    // set minimum level to log
                    .SetMinimumLevel(LogLevel.Debug)
                );
            Logger = loggerFactory.CreateLogger<SemVerEvaluator>();
        }


        const string OperatorEqual = "=";
        const string OperatorNotEqual = "!=";
        const string OperatorLess = "<";
        const string OperatorLessOrEqual = "<=";
        const string OperatorGreater = ">";
        const string OperatorGreaterOrEqual = ">=";
        const string OperatorMatchMajor = "^";
        const string OperatorMatchMinor = "~";

        internal object Evaluate(IProcessJsonLogic p, JToken[] args, object data)
        {
            // check if we have at least 3 arguments
            if (args.Length < 3)
            {
                return false;
            }
            // get the value from the provided evaluation context
            var versionString = p.Apply(args[0], data).ToString();

            // get the operator
            var semVerOperator = p.Apply(args[1], data).ToString();

            // get the target version
            var targetVersionString = p.Apply(args[2], data).ToString();

            //convert to semantic versions
            try
            {
                var version = SemVersion.Parse(versionString, SemVersionStyles.Strict);
                var targetVersion = SemVersion.Parse(targetVersionString, SemVersionStyles.Strict);

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
            catch (Exception e)
            {
                Logger?.LogDebug("Exception during SemVer evaluation: " + e.Message);
                return false;
            }
        }
    }
}