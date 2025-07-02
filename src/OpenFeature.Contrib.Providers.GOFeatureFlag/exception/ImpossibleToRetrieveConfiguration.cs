using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception;

/// <summary>
///     Thrown when it is impossible to retrieve the flag configuration.
/// </summary>
/// <param name="message">Message associated with the exception.</param>
/// <param name="e"></param>
public class ImpossibleToRetrieveConfiguration(string message, Exception e = null) : GoFeatureFlagException(message, e)
{
}
