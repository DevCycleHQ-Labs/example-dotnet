namespace HelloTogglebot
{
    using System;
    using DevCycle.SDK.Server.Local.Api;
    using DevCycle.SDK.Server.Common.API;
    using DevCycle.SDK.Server.Common.Model;
    using DevCycle.SDK.Server.Common.Model.Local;

    public class DevCycleClient
    {
        private static DevCycleLocalClient? client;
        private static bool initialized = false;

        public static async Task Initialize()
        {
            var DEVCYCLE_SDK_KEY = System.Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY");
            if (DEVCYCLE_SDK_KEY == null)
            {
                Console.WriteLine("DEVCYCLE_SERVER_SDK_KEY environment variable not set");
                return;
            }

            // Initialize the DevCycle SDK client
            client = new DevCycleLocalClientBuilder()
                .SetSDKKey(DEVCYCLE_SDK_KEY)
                .SetOptions(
                    new DevCycleLocalOptions(configPollingIntervalMs: 5000, eventFlushIntervalMs: 1000)
                )
                .SetInitializedSubscriber((o, e) =>
                {
                    if (e.Success)
                    {
                        initialized = true;
                    }
                    else
                    {
                        Console.WriteLine($"DevCycle Client did not initialize. Errors: {e.Errors}");
                    }
                })
                .Build();

            try
            {
                await Task.Delay(5000);
            }
            catch (TaskCanceledException)
            {
                System.Environment.Exit(0);
            }
        }

        public static DevCycleLocalClient GetClient()
        {
            if (!initialized || client == null)
            {
                throw new Exception("DevCycle Client not initialized");
            }
            return client;
        }
    }
}
