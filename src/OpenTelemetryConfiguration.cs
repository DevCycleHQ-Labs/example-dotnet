namespace HelloTogglebot
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Resources;

    public static class OpenTelemetryConfiguration
    {
        public static void Configure(WebApplicationBuilder builder)
        {
            if (DynatraceConfiguration.IsConfigured)
            {
                Console.WriteLine($"Dynatrace configured - Service: {DynatraceConfiguration.ServiceName}, Endpoint: {DynatraceConfiguration.GetOtlpEndpoint()}");

                builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(
                            serviceName: DynatraceConfiguration.ServiceName,
                            serviceVersion: DynatraceConfiguration.ServiceVersion))
                    .WithTracing(tracing => tracing
                        .AddSource("HelloTogglebot.Pages")
                        .AddSource("HelloTogglebot.Test")
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.Filter = (httpContext) =>
                            {
                                // Skip health check endpoints and static files
                                var path = httpContext.Request.Path.Value?.ToLower();
                                var shouldTrace = !(path?.StartsWith("/health") == true ||
                                        path?.StartsWith("/favicon") == true ||
                                        path?.StartsWith("/css") == true ||
                                        path?.StartsWith("/js") == true ||
                                        path?.StartsWith("/images") == true);
                                Console.WriteLine($"Tracing filter - Path: {path}, ShouldTrace: {shouldTrace}");
                                return shouldTrace;
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(DynatraceConfiguration.GetOtlpEndpoint());
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                            options.Headers = string.Join(",",
                                DynatraceConfiguration.GetHeaders()
                                    .Select(kvp => $"{kvp.Key}={kvp.Value}"));
                            options.TimeoutMilliseconds = 5000;
                            Console.WriteLine($"OTLP Exporter configured - Endpoint: {options.Endpoint}, Headers: {options.Headers}");
                        }));
            }
            else
            {
                Console.WriteLine("Dynatrace configuration not found. Tracing disabled.");
            }
        }

        public static void ConfigureShutdown(WebApplication app)
        {
            // Ensure spans are flushed on shutdown
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("Flushing OpenTelemetry spans...");
                app.Services.GetService<TracerProvider>()?.ForceFlush(5000);
            });
        }
    }
}