namespace HelloTogglebot
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using DevCycle.SDK.Server.Local.Api;
    using DevCycle.SDK.Server.Common.Model;

    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Context.Propagation;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using Dynatrace.OneAgent.Sdk.Api;

    using HelloTogglebot.Hooks;

    public class Program
    {
        static async Task Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            await DevCycleClient.Initialize();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            // Initialize OneAgent SDK
            var oneAgentSdk = OneAgentSdkFactory.CreateInstance();
            builder.Services.AddSingleton<IOneAgentSdk>(oneAgentSdk);
            Console.WriteLine($"OneAgent SDK initialized - State: {oneAgentSdk.CurrentState}");

            var client = DevCycleClient.GetClient();
            client.AddEvalHook(new DynatraceSpanHook(oneAgentSdk));
            client.AddEvalHook(new ActivityHook(new ActivitySource("HelloPaulTest")));
            // Configure OpenTelemetry with Dynatrace
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();

            app.Use(async (context, next) =>
            {
                // Define the user object for the request
                DevCycleUser user = new DevCycleUser("a_unique_id");
                context.Items["user"] = user;

                await next();
            });

            VariationLogger.Start();

            // Ensure spans are flushed on shutdown
            var lifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("Flushing OpenTelemetry spans...");
                app.Services.GetService<TracerProvider>()?.ForceFlush(5000);
            });

            app.Run();
        }
    }
}
