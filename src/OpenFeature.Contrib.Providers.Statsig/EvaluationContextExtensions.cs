using OpenFeature.Model;
using Statsig;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFeature.Contrib.Providers.Statsig
{
    internal static class EvaluationContextExtensions
    {

        public static StatsigUser AsStatsigUser(this EvaluationContext user)
        {
            return new StatsigUser();
        }

    }
}
