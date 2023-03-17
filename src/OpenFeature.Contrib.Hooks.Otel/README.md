# OpenFeature OpenTelemetry hook for .NET

### Requirements

- open-feature/dotnet-sdk >= v1.0

## Usage

For this hook to function correctly a global `TracerProvider` must be set, an example of how to do this can be found below.

The `open telemetry hook` taps into the after and error methods of the hook lifecycle to write `events` and `attributes` to an existing `span`.
For this, an active span must be set in the `Tracer`, otherwise the hook will no-op.

### Example
The following example demonstrates the use of the `OpenTelemetry hook` with the `OpenFeature dotnet-sdk`. The traces are sent to a `zipkin` server running at `:9411` which will receive the following trace:

```json
{
	"traceId":"ac4464e6387c552b4b55ab3d19bf64f9",
	"id":"f677ca41dbfd6bfe",
	"name":"run",
	"timestamp":1673431556236064,
	"duration":45,
	"localEndpoint":{
		"serviceName":"hook-example"
		},
		"annotations":[
			{
				"timestamp":1673431556236107,
				"value":"feature_flag: {\"feature_flag.key\":\"my-bool-flag\",\"feature_flag.provider_name\":\"NoopProvider\",\"feature_flag.variant\":\"default-variant\"}"
			}
		],
		"tags":{
			"otel.library.name":"test-tracer",
			"service.name":"hook-example"
		}
}
```

## License

Apache 2.0 - See [LICENSE](./../../LICENSE) for more information.
