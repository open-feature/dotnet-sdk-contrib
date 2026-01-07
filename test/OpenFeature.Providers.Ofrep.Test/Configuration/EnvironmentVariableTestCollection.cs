using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Configuration;

/// <summary>
/// Collection definition for tests that manipulate environment variables.
/// Tests in this collection run sequentially to avoid race conditions.
/// </summary>
[CollectionDefinition("EnvironmentVariableTests", DisableParallelization = true)]
public class EnvironmentVariableTestCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
