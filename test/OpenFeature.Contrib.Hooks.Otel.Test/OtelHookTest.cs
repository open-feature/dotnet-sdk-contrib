using Xunit;
using System.Diagnostics;
using OpenFeature.Model;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using OpenTelemetry.Exporter;

namespace OpenFeature.Contrib.Hooks.Otel.Test
{
    public class OtelHookTest
    {
        [Fact]
        public void TestAfter()
        {
            // List that will be populated with the traces by InMemoryExporter
            var exportedItems = new List<Activity>();

            // Create a new in-memory exporter
            var exporter = new InMemoryExporter<Activity>(exportedItems);

            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("my-tracer")
                    .ConfigureResource(r => r.AddService("inmemory-test"))
                    .AddInMemoryExporter(exportedItems)
                    .Build();


            var tracer = tracerProvider.GetTracer("my-tracer");

            var span = tracer.StartActiveSpan("my-span");

            var otelHook = new OtelHook();

            var evaluationContext = OpenFeature.Model.EvaluationContext.Empty;

            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            var hookTask = otelHook.After<string>(ctx, new FlagEvaluationDetails<string>("my-flag", "foo", Constant.ErrorType.None, "STATIC", "default"), new Dictionary<string, object>());

            Assert.True(hookTask.IsCompleted);

            span.End();

            Assert.Single(exportedItems);

            var rootSpan = exportedItems[0];

            Assert.Single(rootSpan.Events);
            
            var eventsEnum = rootSpan.Events.GetEnumerator();
            eventsEnum.MoveNext();

            ActivityEvent ev = (ActivityEvent)eventsEnum.Current;
            Assert.Equal("feature_flag", ev.Name);

            bool foundFlagKey = false; 
            bool foundFlagVariant = false; 
            bool foundFlagProviderName = false;
            var tagsEnum = ev.Tags.GetEnumerator();

            while (tagsEnum.MoveNext())
            {
                var tag = (KeyValuePair<string, object>)tagsEnum.Current;

                switch (tag.Key)
                {
                    case "feature_flag.key": 
                        foundFlagKey = true;
                        Assert.Equal("my-flag", tag.Value);
                        break;
                    case "feature_flag.variant": 
                        foundFlagVariant = true;
                        Assert.Equal("default", tag.Value);
                        break;
                    case "feature_flag.provider_name": 
                        foundFlagProviderName = true;
                        Assert.Equal("my-provider", tag.Value);
                        break;
                    default: break;
                }
            }

            Assert.True(foundFlagKey);
            Assert.True(foundFlagVariant);
            Assert.True(foundFlagProviderName);
        }

        [Fact]
        public void TestAfterNoSpan()
        {
            // List that will be populated with the traces by InMemoryExporter
            var exportedItems = new List<Activity>();

            // Create a new in-memory exporter
            var exporter = new InMemoryExporter<Activity>(exportedItems);

            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("my-tracer")
                    .ConfigureResource(r => r.AddService("inmemory-test"))
                    .AddInMemoryExporter(exportedItems)
                    .Build();


            var tracer = tracerProvider.GetTracer("my-tracer");

            var otelHook = new OtelHook();

            var evaluationContext = OpenFeature.Model.EvaluationContext.Empty;

            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            var hookTask = otelHook.After<string>(ctx, new FlagEvaluationDetails<string>("my-flag", "foo", Constant.ErrorType.None, "STATIC", "default"), new Dictionary<string, object>());

            Assert.True(hookTask.IsCompleted);

            Assert.Empty(exportedItems);
        }

        [Fact]
        public void TestError()
        {
            // List that will be populated with the traces by InMemoryExporter
            var exportedItems = new List<Activity>();

            // Create a new in-memory exporter
            var exporter = new InMemoryExporter<Activity>(exportedItems);

            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("my-tracer")
                    .ConfigureResource(r => r.AddService("inmemory-test"))
                    .AddInMemoryExporter(exportedItems)
                    .Build();


            var tracer = tracerProvider.GetTracer("my-tracer");

            var span = tracer.StartActiveSpan("my-span");

            var otelHook = new OtelHook();

            var evaluationContext = OpenFeature.Model.EvaluationContext.Empty;

            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            var hookTask = otelHook.Error<string>(ctx, new System.Exception("unexpected error"), new Dictionary<string, object>());

            Assert.True(hookTask.IsCompleted);

            span.End();

            Assert.Single(exportedItems);

            var rootSpan = exportedItems[0];

            Assert.Single(rootSpan.Events);

            var enumerator = rootSpan.Events.GetEnumerator();
            enumerator.MoveNext();
            var ev = (ActivityEvent)enumerator.Current;

            Assert.Equal("exception", ev.Name);

            bool foundExceptionMessage = false;

            var tagsEnum = ev.Tags.GetEnumerator();

            while (tagsEnum.MoveNext())
            {
                var tag = (KeyValuePair<string, object>)tagsEnum.Current;

                switch (tag.Key)
                {
                    case "exception.message": 
                        foundExceptionMessage = true;
                        Assert.Equal("unexpected error", tag.Value);
                        break;
                    default: break;
                }
            }

            Assert.True(foundExceptionMessage);
        }

        [Fact]
        public void TestErrorNoSpan()
        {
            // List that will be populated with the traces by InMemoryExporter
            var exportedItems = new List<Activity>();

            // Create a new in-memory exporter
            var exporter = new InMemoryExporter<Activity>(exportedItems);

            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("my-tracer")
                    .ConfigureResource(r => r.AddService("inmemory-test"))
                    .AddInMemoryExporter(exportedItems)
                    .Build();


            var tracer = tracerProvider.GetTracer("my-tracer");

            var otelHook = new OtelHook();

            var evaluationContext = OpenFeature.Model.EvaluationContext.Empty;

            var ctx = new HookContext<string>("my-flag", "foo", Constant.FlagValueType.String, new ClientMetadata("my-client", "1.0"), new Metadata("my-provider"), evaluationContext);

            var hookTask = otelHook.Error<string>(ctx, new System.Exception("unexpected error"), new Dictionary<string, object>());

            Assert.True(hookTask.IsCompleted);

            Assert.Empty(exportedItems);
        }
    }
}

