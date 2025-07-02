namespace OpenFeature.Providers.GOFeatureFlag.exception;

/// <summary>
///     Exception throw when the options of the provider are invalid.
/// </summary>
public class InvalidOptionException : GoFeatureFlagException
{
    /// <summary>
    ///     Constructor of the exception
    /// </summary>
    /// <param name="message">Message to display</param>
    public InvalidOptionException(string message) : base(message)
    {
    }
}
