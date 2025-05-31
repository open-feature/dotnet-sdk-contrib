namespace OpenFeature.Providers.Ofrep.Client.Exceptions;

/// <summary>
/// Exception thrown when there is a configuration error in the OFREP client.
/// </summary>
public class OfrepConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfrepConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OfrepConfigurationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
