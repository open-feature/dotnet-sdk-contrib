using System;

namespace OpenFeature.Providers.GOFeatureFlag.exception;

/// <summary>
///     Thrown when it is impossible to send data to the GO Feature Flag collector.
/// </summary>
/// <param name="message">Message associated with the exception.</param>
/// <param name="e"></param>
public class ImpossibleToSendDataToTheCollector(string message, Exception e = null) : GoFeatureFlagException(message, e)
{
}
