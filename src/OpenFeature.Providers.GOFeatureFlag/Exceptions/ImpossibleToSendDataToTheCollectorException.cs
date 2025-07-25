using System;

namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     Thrown when it is impossible to send data to the GO Feature Flag collector.
/// </summary>
/// <param name="message">Message associated with the exception.</param>
/// <param name="e"></param>
public class ImpossibleToSendDataToTheCollectorException(string message, Exception? e = null) : GOFeatureFlagException(message, e)
{
}
