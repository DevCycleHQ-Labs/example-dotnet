namespace HelloTogglebot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Logs;

    public static class OtelConfiguration
    {
        public static void Configure(WebApplicationBuilder builder)
        {
            // Check if OpenTelemetry is enabled
            var otelEnabled = Environment.GetEnvironmentVariable("OTEL_ENABLED");
            if (string.IsNullOrEmpty(otelEnabled) || !bool.TryParse(otelEnabled, out var enabled) || !enabled)
            {
                Console.WriteLine("OpenTelemetry disabled (OTEL_ENABLED not set to true).");
                return;
            }

            var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "HelloTogglebot";
            var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "1.0.0";
            var otlpEndpoint = GetOtlpEndpointFromEnv();
            var headers = GetHeadersFromEnv();
            var tracingSources = new[] { "HelloTogglebot.Pages", "HelloTogglebot.Test", "DevCycle.FlagEvaluations" };

            if (string.IsNullOrEmpty(otlpEndpoint))
            {
                Console.WriteLine("OpenTelemetry endpoint not configured. Tracing disabled.");
                return;
            }

            Configure(builder, serviceName, serviceVersion, otlpEndpoint, headers, tracingSources);
        }

        public static void Configure(WebApplicationBuilder builder,
            string serviceName,
            string serviceVersion,
            string otlpEndpoint,
            Dictionary<string, string> headers,
            string[] tracingSources)
        {
            Console.WriteLine($"OpenTelemetry configured - Service: {serviceName}, Endpoint: {otlpEndpoint}");

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(
                        serviceName: serviceName,
                        serviceVersion: serviceVersion))
                .WithTracing(tracing =>
                {
                    var tracingBuilder = tracing;

                    // Add custom sources
                    foreach (var source in tracingSources)
                    {
                        tracingBuilder = tracingBuilder.AddSource(source);
                    }

                    tracingBuilder
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
                            options.Endpoint = new Uri(otlpEndpoint);
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                            if (headers.Any())
                            {
                                options.Headers = string.Join(",", headers.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                            }
                            options.TimeoutMilliseconds = 5000;
                            Console.WriteLine($"OTLP Exporter configured - Endpoint: {options.Endpoint}, Headers: {options.Headers}");
                        });
                });
            builder.Logging.AddOpenTelemetry(opt =>
            {
                opt.IncludeFormattedMessage = true; // Include the formatted log message
                opt.IncludeScopes = true; // Include scope information
                opt.ParseStateValues = true; // Enable structured log parsing;
                opt.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("https://xtc47953.live.dynatrace.com/api/v2/otlp/v1/logs");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    if (headers.Any())
                    {
                        options.Headers = string.Join(",", headers.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    }
                });
            });
        }



        private static string? GetOtlpEndpointFromEnv()
        {
            var endpoint = Environment.GetEnvironmentVariable("DT_ENDPOINT");
            if (string.IsNullOrEmpty(endpoint))
            {
                return Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            }

            return $"{endpoint.TrimEnd('/')}/api/v2/otlp/v1/traces";
        }

        private static Dictionary<string, string> GetHeadersFromEnv()
        {
            var headers = new Dictionary<string, string>();

            // Check for Dynatrace API token
            var dtApiToken = Environment.GetEnvironmentVariable("DT_API_TOKEN");
            if (!string.IsNullOrEmpty(dtApiToken))
            {
                headers["Authorization"] = $"Api-Token {dtApiToken}";
            }

            // Check for generic OTEL headers
            var otelHeaders = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
            if (!string.IsNullOrEmpty(otelHeaders))
            {
                foreach (var header in otelHeaders.Split(','))
                {
                    var parts = header.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        headers[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            return headers;
        }
    }
}
