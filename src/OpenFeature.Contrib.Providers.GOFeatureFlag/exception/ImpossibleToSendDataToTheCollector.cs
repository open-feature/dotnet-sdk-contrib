using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception;

/// <summary>
///     Thrown when it is impossible to send data to the GO Feature Flag collector.
/// </summary>
/// <param name="message">Message associated with the exception.</param>
public class ImpossibleToSendDataToTheCollector(string message, Exception e = null) : GoFeatureFlagException(message, e)
{
}
