using System;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
public class FlagdStepDefinitionsRpc
{
    private readonly TestContext _context;

    public FlagdStepDefinitionsRpc(TestContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        this._context.ProviderResolverType = "Rpc";
    }
}
