using System;

namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     Unauthorized exception is thrown when the user is not authorized to access a resource.
/// </summary>
/// <param name="message">message</param>
/// <param name="e">child exception</param>
public class UnauthorizedException(string message, Exception e = null) : GOFeatureFlagException(message, e)
{
}
