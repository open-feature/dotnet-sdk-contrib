using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using OpenFeature.Model;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenFeature.Contrib.Hooks.Otel.Test
{
    public class MetricsHookTest
    {
        [Fact]
        public void After_Test()
        {
            // Arrange metrics collector
            var exportedItems = new List<Metric>();
            Sdk.CreateMeterProviderBuilder()
                .AddMeter("*")
                .ConfigureResource(r => r.AddService("openfeature"))
                .AddInMemoryExporter(exportedItems, option => option.PeriodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions { ExportIntervalMilliseconds = 100 })
                .Build();

            // Arrange
            const string metricName = "feature_flag.evaluation_success_total";
            var otelHook = new MetricsHook();
            var evaluationContext = EvaluationContext.Empty;
            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            // Act
            var hookTask = otelHook.After(ctx, new FlagEvaluationDetails<string>("my-flag", "foo", Constant.ErrorType.None, "STATIC", "default"), new Dictionary<string, object>());
            // Wait for the metrics to be exported
            Thread.Sleep(150);

            // Assert
            Assert.True(hookTask.IsCompleted);
            
            // Assert metrics
            Assert.NotEmpty(exportedItems);
            
            // check if the metric feature_flag.evaluation_success_total is present in the exported items
            var metric = exportedItems.FirstOrDefault(m => m.Name == metricName);
            Assert.NotNull(metric);
            
            var noOtherMetric = exportedItems.All(m => m.Name == metricName);
            Assert.True(noOtherMetric);
        }

        [Fact]
        public void Error_Test()
        {
            // Arrange metrics collector
            var exportedItems = new List<Metric>();
            Sdk.CreateMeterProviderBuilder()
                .AddMeter("*")
                .ConfigureResource(r => r.AddService("openfeature"))
                .AddInMemoryExporter(exportedItems, option => option.PeriodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions { ExportIntervalMilliseconds = 100 })
                .Build();

            // Arrange
            const string metricName = "feature_flag.evaluation_error_total";
            var otelHook = new MetricsHook();
            var evaluationContext = EvaluationContext.Empty;
            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            // Act
            var hookTask = otelHook.Error(ctx, new Exception(), new Dictionary<string, object>());
            // Wait for the metrics to be exported
            Thread.Sleep(150);

            // Assert
            Assert.True(hookTask.IsCompleted);
            
            // Assert metrics
            Assert.NotEmpty(exportedItems);
            
            // check if the metric feature_flag.evaluation_success_total is present in the exported items
            var metric = exportedItems.FirstOrDefault(m => m.Name == metricName);
            Assert.NotNull(metric);
            
            var noOtherMetric = exportedItems.All(m => m.Name == metricName);
            Assert.True(noOtherMetric);
        }
        
        [Fact]
        public void Finally_Test()
        {
            // Arrange metrics collector
            var exportedItems = new List<Metric>();
            Sdk.CreateMeterProviderBuilder()
                .AddMeter("*")
                .ConfigureResource(r => r.AddService("openfeature"))
                .AddInMemoryExporter(exportedItems, option => option.PeriodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions { ExportIntervalMilliseconds = 100 })
                .Build();

            // Arrange
            const string metricName = "feature_flag.evaluation_active_count";
            var otelHook = new MetricsHook();
            var evaluationContext = EvaluationContext.Empty;
            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            // Act
            var hookTask = otelHook.Finally(ctx, new Dictionary<string, object>());
            // Wait for the metrics to be exported
            Thread.Sleep(150);

            // Assert
            Assert.True(hookTask.IsCompleted);
            
            // Assert metrics
            Assert.NotEmpty(exportedItems);
            
            // check if the metric feature_flag.evaluation_success_total is present in the exported items
            var metric = exportedItems.FirstOrDefault(m => m.Name == metricName);
            Assert.NotNull(metric);
            
            var noOtherMetric = exportedItems.All(m => m.Name == metricName);
            Assert.True(noOtherMetric);
        }
    }
}
