namespace HelloTogglebot
{
    using System;
    using System.IO;

    using DevCycle.SDK.Server.Local.Api;
    using DevCycle.SDK.Server.Common.API;
    using DevCycle.SDK.Server.Common.Model;
    using DevCycle.SDK.Server.Common.Model.Local;

    public static class VariationLogger
    {
        /**
        * Since this is used outside of a request context, we define a service user.
        * This can contian properties unique to this service, and allows you to target
        * services in the same way you would target app users.
        */
        static DevCycleUser ServiceUser = new DevCycleUser("api-user");

        public static async void Start()
        {
            await LogVariation(0);
        }

        public static async Task LogVariation(int idx)
        {
            DevCycleLocalClient client = DevCycleClient.GetClient();
            var features = await client.AllFeatures(ServiceUser);
            string variationName = features.ContainsKey("hello-togglebot")
                ? features["hello-togglebot"].VariationName
                : "Default";

            bool wink = await client.VariableValue(ServiceUser, "togglebot-wink", false);
            string speed = await client.VariableValue(ServiceUser, "togglebot-speed", "off");

            string spinChars = speed == "slow" ? "◟◜◝◞" : "◜◠◝◞◡◟";
            string spinner = speed == "off" ? "○" : spinChars[idx % spinChars.Length].ToString();
            idx = (idx + 1) % spinChars.Length;

            string face = wink ? "(- ‿ ○)" : "(○ ‿ ○)";

            string frame = $"{spinner} Serving variation: {variationName} {face}";
            string color = speed == "surprise" ? "rainbow" : "blue";
            WriteToConsole(frame, color);

            int timeout = speed == "off" || speed == "slow" ? 500 : 100;
            await Task.Delay(timeout);

            await LogVariation(idx);
        }

        private static void WriteToConsole(string frame, string color)
        {
            var colors = new ConsoleColor[]
            {
                ConsoleColor.Red,
                ConsoleColor.Green,
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
                ConsoleColor.Cyan,
                ConsoleColor.Magenta,
            };
            Console.ForegroundColor = color == "rainbow"
                ? colors[(DateTime.Now.Ticks % (colors.Length * 2)) / 2]
                : ConsoleColor.Blue;
            Console.Write($"\x1b[K  {frame}\r");
            Console.ResetColor();
        }
    }
}