using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenFeature.Model;

namespace OpenFeature.Providers.Ofrep.Extensions;

/// <summary>
/// Extension methods for EvaluationContext class
/// </summary>
public static class EvaluationContextExtensions
{
    // Dedicated options for ETag generation to avoid creating new instances
    private static readonly JsonSerializerOptions EtagJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    /// <summary>
    /// Generates a strong ETag based on the provided evaluation context.
    /// </summary>
    /// <param name="context">The evaluation context to generate an ETag for</param>
    /// <returns>A strong ETag representing the context</returns>
    public static string GenerateETag(this EvaluationContext context)
    {
        var contextDict = ToDictionary(context);

        // Check if context is null or empty and return a default ETag if so
        if (contextDict.Count == 0)
        {
            // Initialize with the correct nullable type if empty
            contextDict = new Dictionary<string, object?>();
        }

        // 1. Canonicalize by sorting keys and reserializing
        var sortedContext = new SortedDictionary<string, object?>(contextDict, StringComparer.Ordinal);

        // Use the cached serialization options for ETag generation
        string canonicalJson = JsonSerializer.Serialize(sortedContext, EtagJsonOptions);

        // 2. Cryptographic Hash (SHA-256 recommended)
        byte[] jsonBytes = Encoding.UTF8.GetBytes(canonicalJson);
        byte[] hashBytes;
        using (var sha256 = SHA256.Create())
        {
            hashBytes = sha256.ComputeHash(jsonBytes);
        }

        // 3. Format as ETag (Base64, enclosed in quotes for a 'strong' ETag)
        string base64Hash = Convert.ToBase64String(hashBytes);
        return $"\"{base64Hash}\""; // Strong ETag format
    }

    /// <summary>
    /// Converts the EvaluationContext to a dictionary of string keys and object values.
    /// </summary>
    /// <param name="context">the evaluation context</param>
    /// <returns>A dictionary representation of the evaluation context.</returns>
    public static Dictionary<string, object?> ToDictionary(this EvaluationContext context)
    {
        return context.AsDictionary().ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.AsObject
        );
    }
}
