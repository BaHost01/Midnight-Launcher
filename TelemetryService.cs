using System;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Midnight_Launcher;

public static class TelemetryService
{
    private static TracerProvider? _tracerProvider;

    public static void Initialize()
    {
        try
        {
            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MidnightLauncher"))
                .AddSource("MidnightLauncher.*")
                .AddHttpClientInstrumentation()
                .AddConsoleExporter() // In production, you'd use OTLP or another exporter
                .Build();
        }
        catch { }
    }

    public static void Shutdown()
    {
        _tracerProvider?.Dispose();
    }
}
