using System.Text.Json;

using OpenFeature.Model;

using Xunit;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test;

public class GoFeatureFlagUserTest
{
    [Fact]
    public void GoFeatureFlagUserSerializesCorrectly()
    {
        var userContext = EvaluationContext.Builder()
            .Set("targetingKey", "1d1b9238-2591-4a47-94cf-d2bc080892f1")
            .Set("firstname", "john")
            .Set("lastname", "doe")
            .Set("email", "john.doe@gofeatureflag.org")
            .Set("admin", true)
            .Set("anonymous", false)
            .Build();

        GoFeatureFlagUser user = userContext;

        var userAsString = JsonSerializer.Serialize(user, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Contains("{\"key\":\"1d1b9238-2591-4a47-94cf-d2bc080892f1\",\"anonymous\":false,\"custom\":{", userAsString);
    }
}