using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.Model;

namespace HelloTogglebot.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private static readonly ActivitySource ActivitySource = new("HelloTogglebot.Pages");

    public string Speed { get; private set; } = "off";
    public bool Wink { get; private set; } = false;

    public string Message { get; private set; } = "";
    public string VariationName { get; private set; } = "Default";
    public string TogglebotSrc { get; private set; } = "";

    public string Header { get; private set; } = "";
    public string Body { get; private set; } = "";

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public async void OnGet()
    {
        using var activity = ActivitySource.StartActivity("IndexModel.OnGet");
        _logger.LogInformation("Starting OnGet method - Activity: {ActivityId}", activity?.Id);

        // Get the user defined on the request context
        DevCycleUser? user = (DevCycleUser?)HttpContext.Items["user"];

        if (user == null)
        {
            throw new Exception("User not defined in request context");
        }

        DevCycleLocalClient client = DevCycleClient.GetClient();
        Dictionary<string, Feature> features = await client.AllFeatures(user);
        VariationName = features.ContainsKey("hello-togglebot")
            ? features["hello-togglebot"].VariationName
            : "Default";

        Wink = await client.VariableValue(user, "togglebot-wink", false);
        Speed = await client.VariableValue(user, "togglebot-speed", "off");

        switch (Speed)
        {
            case "slow":
                Message = "Awesome, look at you go!";
                break;
            case "fast":
                Message = "This is fun!";
                break;
            case "off-axis":
                Message = "...I'm gonna be sick...";
                break;
            case "surprise":
                Message = "What the unicorn?";
                break;
            default:
                Message = "Hello! Nice to meet you.";
                break;
        }

        TogglebotSrc = Wink ? "/images/togglebot-wink.svg" : "/images/togglebot.svg";
        if (Speed == "surprise")
        {
            TogglebotSrc = "/images/unicorn.svg";
        }

        string step = await client.VariableValue(user, "example-text", "default");

        switch (step)
        {
            case "step-1":
                Header = "Welcome to DevCycle's example app.";
                Body = "If you got here through the onboarding flow, just follow the instructions to change and create new Variations and see how the app reacts to new Variable values.";
                break;
            case "step-2":
                Header = "Great! You've taken the first step in exploring DevCycle.";
                Body = "You've successfully toggled your very first Variation. You are now serving a different value to your users and you can see how the example app has reacted to this change. Next, go ahead and create a whole new Variation to see what else is possible in this app.";
                break;
            case "step-3":
                Header = "You're getting the hang of things.";
                Body = "By creating a new Variation with new Variable values and toggling it on for all users, you've already explored the fundamental concepts within DevCycle. There's still so much more to the platform, so go ahead and complete the onboarding flow and play around with the feature that controls this example in your dashboard.";
                break;
            default:
                Header = "Welcome to DevCycle's example app.";
                Body = "If you got to the example app on your own, follow our README guide to create the Feature and Variables you need to control this app in DevCycle.";
                break;
        }
    }
}
