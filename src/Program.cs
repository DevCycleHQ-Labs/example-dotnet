namespace HelloTogglebot
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using DevCycle.SDK.Server.Common.Model;

    using HelloTogglebot.Hooks;
    using Dynatrace.OneAgent.Sdk.Api;
    using OpenTelemetry.Logs;

    public class Program
    {
        static async Task Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            await DevCycleClient.Initialize();

            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.AddOpenTelemetry(opt =>
            {
                opt.IncludeFormattedMessage = true; // Include the formatted log message
                opt.IncludeScopes = true; // Include scope information
                opt.ParseStateValues = true; // Enable structured log parsing;
                opt.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("https://xtc47953.live.dynatrace.com/api/v2/otlp/v1/logs");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                });
            });
            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            // Initialize OneAgent SDK
            var oneAgentSdk = OneAgentSdkFactory.CreateInstance();
            builder.Services.AddSingleton<IOneAgentSdk>(oneAgentSdk);
            Console.WriteLine($"OneAgent SDK initialized - State: {oneAgentSdk.CurrentState}");


            OtelConfiguration.Configure(builder);
            var app = builder.Build();

            var client = DevCycleClient.GetClient();
            // client.AddEvalHook(new ActivityHook(new ActivitySource("DevCycle.FlagEvaluations")));
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            client.AddEvalHook(new LogHook(logger));

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

            // VariationLogger.Start();

            app.Run();
        }
    }
}
