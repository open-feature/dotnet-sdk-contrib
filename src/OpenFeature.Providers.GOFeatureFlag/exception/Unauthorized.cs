using System;

namespace OpenFeature.Providers.GOFeatureFlag.exception;

/// <summary>
///     Unauthorized exception is thrown when the user is not authorized to access a resource.
/// </summary>
/// <param name="message">message</param>
/// <param name="e">child exception</param>
public class Unauthorized(string message, Exception e = null) : GoFeatureFlagException(message, e)
{
}
