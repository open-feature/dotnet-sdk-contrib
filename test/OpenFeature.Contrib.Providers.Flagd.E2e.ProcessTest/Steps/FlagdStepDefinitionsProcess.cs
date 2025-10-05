using System;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps;

[Binding]
public class FlagdStepDefinitionsProcess
{
    private readonly TestContext _context;

    public FlagdStepDefinitionsProcess(TestContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        this._context.ProviderResolverType = "InProcess";
    }
}
