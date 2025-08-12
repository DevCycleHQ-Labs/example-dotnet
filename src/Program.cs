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
                        .SetSampler(new AlwaysOnSampler())
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                        })
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter()
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

                builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);

                // Enable OpenTelemetry internal logging
                builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);
                builder.Logging.AddFilter("System.Net.Http", LogLevel.Debug);
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
