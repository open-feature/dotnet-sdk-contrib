using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonLogic.Net;
using Microsoft.Extensions.Logging;
using Murmur;
using Newtonsoft.Json.Linq;
using Semver;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    /// <inheritdoc/>
    public class FractionalEvaluator
    {

        internal ILogger Logger { get; set; }

        internal FractionalEvaluator()
        {
            var loggerFactory = LoggerFactory.Create(
                builder => builder
                    // add console as logging target
                    .AddConsole()
                    // add debug output as logging target
                    .AddDebug()
                    // set minimum level to log
                    .SetMinimumLevel(LogLevel.Debug)
                );
            Logger = loggerFactory.CreateLogger<FractionalEvaluator>();
        }

        class FractionalEvaluationDistribution
        {
            public string variant;
            public int weight;
        }

        internal object Evaluate(IProcessJsonLogic p, JToken[] args, object data)
        {
            // check if we have at least two arguments:
            // 1. the property value
            // 2. the array containing the buckets

            if (args.Length == 0)
            {
                return null;
            }

            var flagdProperties = new FlagdProperties(data);

            // check if the first argument is a string (i.e. the property to base the distribution on
            var propertyValue = flagdProperties.TargetingKey;
            var bucketStartIndex = 0;

            var arg0 = p.Apply(args[0], data);

            if (arg0 is string stringValue)
            {
                propertyValue = stringValue;
                bucketStartIndex = 1;
            }

            var distributions = new List<FractionalEvaluationDistribution>();
            var distributionSum = 0;

            for (var i = bucketStartIndex; i < args.Length; i++)
            {
                var bucket = p.Apply(args[i], data);

                if (!bucket.IsEnumerable())
                {
                    continue;
                }

                var bucketArr = bucket.MakeEnumerable().ToArray();

                if (!bucketArr.Any())
                {
                    continue;
                }

                var weight = 1;

                if (bucketArr.Length >= 2 && bucketArr.ElementAt(1).IsNumeric())
                {
                    weight = Convert.ToInt32(bucketArr.ElementAt(1));
                }

                distributions.Add(new FractionalEvaluationDistribution
                {
                    variant = bucketArr.ElementAt(0).ToString(),
                    weight = weight
                });

                distributionSum += weight;
            }

            var valueToDistribute = flagdProperties.FlagKey + propertyValue;
            var murmur32 = MurmurHash.Create32();
            var bytes = Encoding.ASCII.GetBytes(valueToDistribute);
            var hashBytes = murmur32.ComputeHash(bytes);
            var hash = BitConverter.ToInt32(hashBytes, 0);

            var bucketValue = (int)(Math.Abs((float)hash) / Int32.MaxValue * 100);

            var rangeEnd = 0.0;

            foreach (var dist in distributions)
            {
                rangeEnd += 100 * (dist.weight / (float)distributionSum);
                if (bucketValue < rangeEnd)
                {
                    return dist.variant;
                }
            }

            Logger.LogDebug("No matching bucket found");
            return "";
        }
    }
}