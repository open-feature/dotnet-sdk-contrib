using System;
using System.Collections.Generic;
using System.Linq;
using OpenFeature.Model;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenFeature.Contrib.Hooks.Otel.Test
{
    public class MetricsHookTest
    {
        readonly List<Metric> exportedItems;
        readonly MeterProvider meterProvider;
        HookContext<string> hookContext = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), EvaluationContext.Empty);

        public MetricsHookTest()
        {
            exportedItems = new List<Metric>();
            meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("*")
                .ConfigureResource(r => r.AddService("openfeature"))
                .AddInMemoryExporter(exportedItems)
                .Build();
        }

        [Fact]
        public async void After_Test()
        {
            // Arrange
            const string metricName = "feature_flag.evaluation_success_total";
            var otelHook = new MetricsHook();

            // Act
            await otelHook.After(hookContext, new FlagEvaluationDetails<string>("my-flag", "foo", Constant.ErrorType.None, "STATIC", "default"), new Dictionary<string, object>());

            // Flush metrics
            meterProvider.ForceFlush();

            // Assert metrics
            Assert.NotEmpty(exportedItems);

            // check if the metric is present in the exported items
            var metric = exportedItems.FirstOrDefault(m => m.Name == metricName);
            Assert.NotNull(metric);

            var noOtherMetric = exportedItems.All(m => m.Name == metricName);
            Assert.True(noOtherMetric);
        }

        [Fact]
        public async void Error_Test()
        {
            // Arrange
            const string metricName = "feature_flag.evaluation_error_total";
            var otelHook = new MetricsHook();

            // Act
            await otelHook.Error(hookContext, new Exception(), new Dictionary<string, object>());

            // Flush metrics
            meterProvider.ForceFlush();

            // Assert metrics
            Assert.NotEmpty(exportedItems);

            // check if the metric is present in the exported items
            var metric = exportedItems.FirstOrDefault(m => m.Name == metricName);
            Assert.NotNull(metric);

            var noOtherMetric = exportedItems.All(m => m.Name == metricName);
            Assert.True(noOtherMetric);
        }

        [Fact]
        public async void Finally_Test()
        {
            // Arrange
            const string metricName = "feature_flag.evaluation_active_count";
            var otelHook = new MetricsHook();

            // Act
            await otelHook.Finally(hookContext, new Dictionary<string, object>());

            // Flush metrics
            meterProvider.ForceFlush();

            // Assert metrics
            Assert.NotEmpty(exportedItems);

            // check if the metric feature_flag.evaluation_success_total is present in the exported items
            var metric = exportedItems.FirstOrDefault(m => m.Name == metricName);
            Assert.NotNull(metric);

            var noOtherMetric = exportedItems.All(m => m.Name == metricName);
            Assert.True(noOtherMetric);
        }

        [Fact]
        public async void Before_Test()
        {

            // Arrange
            const string metricName1 = "feature_flag.evaluation_active_count";
            const string metricName2 = "feature_flag.evaluation_requests_total";
            var otelHook = new MetricsHook();

            // Act
            await otelHook.Before(hookContext, new Dictionary<string, object>());

            // Flush metrics
            meterProvider.ForceFlush();

            // Assert metrics
            Assert.NotEmpty(exportedItems);

            // check if the metric is present in the exported items
            var metric1 = exportedItems.FirstOrDefault(m => m.Name == metricName1);
            Assert.NotNull(metric1);

            var metric2 = exportedItems.FirstOrDefault(m => m.Name == metricName2);
            Assert.NotNull(metric2);

            var noOtherMetric = exportedItems.All(m => m.Name == metricName1 || m.Name == metricName2);
            Assert.True(noOtherMetric);
        }
    }
}
