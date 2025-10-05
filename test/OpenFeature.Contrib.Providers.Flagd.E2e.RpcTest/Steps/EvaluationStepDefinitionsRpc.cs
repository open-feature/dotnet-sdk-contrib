using System;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
public class EvaluationStepDefinitionsRpc
{
    private readonly TestContext _context;

    public EvaluationStepDefinitionsRpc(TestContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        this._context.ProviderResolverType = "Rpc";
    }
}
