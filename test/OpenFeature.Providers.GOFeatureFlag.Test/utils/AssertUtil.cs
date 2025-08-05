using System;
using Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace OpenFeature.Providers.GOFeatureFlag.Test.Utils;

/// <summary>
///     AssertUtil is a utility class that provides methods for asserting conditions in tests.
/// </summary>
public class AssertUtil
{
    public static void JsonEqual(string expectedJson, string actualJson)
    {
        if (string.IsNullOrWhiteSpace(expectedJson) || string.IsNullOrWhiteSpace(actualJson))
        {
            throw new ArgumentException("JSON strings cannot be null or empty.");
        }

        var token1 = JObject.Parse(expectedJson);
        var token2 = JObject.Parse(actualJson);

        if (!JToken.DeepEquals(token1, token2))
        {
            throw new EqualException(expectedJson, actualJson);
        }
    }

    public static void JsonNotEqual(string expectedJson, string actualJson)
    {
        try
        {
            JsonEqual(expectedJson, actualJson);
        }
        catch (EqualException)
        {
            // If EqualException is thrown, it means the JSONs are not equal, which is what we want.
            return;
        }

        throw new XunitException($"Expected JSON: {expectedJson} to not equal actual JSON: {actualJson}");
    }

    private class EqualException : XunitException
    {
        public EqualException(string expected, string actual)
            : base($"Expected JSON: {expected} but found: {actual}")
        {
        }
    }
}
