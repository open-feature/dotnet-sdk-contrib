namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     Exception throw when the options of the provider are invalid.
/// </summary>
public class InvalidOptionException : GOFeatureFlagException
{
    /// <summary>
    ///     Constructor of the exception
    /// </summary>
    /// <param name="message">Message to display</param>
    public InvalidOptionException(string message) : base(message)
    {
    }
}
