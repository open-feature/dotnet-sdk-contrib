using System;

namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     Thrown when it is impossible to retrieve the flag configuration.
/// </summary>
/// <param name="message">Message associated with the exception.</param>
/// <param name="e"></param>
public class ImpossibleToRetrieveConfigurationException(string message, Exception e = null) : GOFeatureFlagException(message, e)
{
}
