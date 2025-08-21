using Microsoft.AspNetCore.Mvc;
using DevCycle.SDK.Server.Common.Model;
using Dynatrace.OneAgent.Sdk.Api;

namespace HelloTogglebot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

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
        var variable5 = await client.VariableAsync(user, "togglebot-wink", true);

        return Ok(new
        {
            message = "Test trace created",
            sdkState = oneAgent.CurrentState.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            variable = variable
        });
    }
}
