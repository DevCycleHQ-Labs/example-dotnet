using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
// using Dynatrace.OneAgent.Sdk.Api;
using DevCycle.SDK.Server.Common.Model;
using Dynatrace.OneAgent.Sdk.Api;

namespace HelloTogglebot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private static readonly ActivitySource ActivitySource = new("HelloTogglebot.Test");

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet("trace")]
    public async Task<IActionResult> TestTraceAsync([FromServices] IOneAgentSdk oneAgent)
    {

        var client = DevCycleClient.GetClient();
        var user = new DevCycleUser("userId");
        var variable = await client.VariableAsync(user, "test", false);
        var variable2 = await client.VariableAsync(user, "test2", false);
        var variable3 = await client.VariableAsync(user, "test", true);
        var variable4 = await client.VariableAsync(user, "test4", true);

        return Ok(new
        {
            message = "Test trace created",
            sdkState = oneAgent.CurrentState.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            variable = variable
        });
    }

    // [HttpGet("oneagent")]
    // public async Task<IActionResult> TestOneAgentAsync([FromServices] IOneAgentSdk oneAgent)
    // {
    //     _logger.LogInformation("OneAgent SDK test endpoint called - State: {State}", oneAgent.CurrentState);
    //
    //     var client = DevCycleClient.GetClient();
    //     var user = new DevCycleUser("userId");
    //     var variable = await client.VariableAsync(user, "test", false);
    //     var variable2 = await client.VariableAsync(user, "test2", false);
    //     var variable3 = await client.VariableAsync(user, "test", false);
    //     var variable4 = await client.VariableAsync(user, "test4", true);
    //
    //     return Ok(new
    //     {
    //         message = "OneAgent SDK available",
    //         sdkState = oneAgent.CurrentState.ToString(),
    //         timestamp = DateTimeOffset.UtcNow,
    //         variable = variable
    //     });
    // }
    //
}
