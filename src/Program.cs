namespace HelloTogglebot
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using DevCycle.SDK.Server.Common.Model;

    using System.Diagnostics;

    using HelloTogglebot.Hooks;
    using Dynatrace.OneAgent.Sdk.Api;

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
            client.AddEvalHook(new ActivityHook(new ActivitySource("DevCycle.FlagEvaluations")));

            // Configure OpenTelemetry
            // OtelConfiguration.Configure(builder);

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

            app.Run();
        }
    }
}
