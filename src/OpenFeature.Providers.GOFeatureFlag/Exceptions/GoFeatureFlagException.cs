using System;

namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     GOFeatureFlagException is the root exception of GO Feature Flag provider.
/// </summary>
public abstract class GOFeatureFlagException : Exception
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public GOFeatureFlagException()
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message of your exception</param>
    public GOFeatureFlagException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message of your exception</param>
    /// <param name="inner">Root exception.</param>
    public GOFeatureFlagException(string message, Exception? inner)
        : base(message, inner)
    {
    }
}
