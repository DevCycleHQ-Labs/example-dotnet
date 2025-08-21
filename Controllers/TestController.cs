using Microsoft.AspNetCore.Mvc;
using DevCycle.SDK.Server.Common.Model;
using Dynatrace.OneAgent.Sdk.Api;
using Newtonsoft.Json.Linq;

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
        JObject parsed = JObject.Parse("{\"name\":\"John\", \"age\":30}");
        var variable = await client.VariableAsync(user, "test", false);
        var variable2 = await client.VariableAsync(user, "json-value", parsed);
        var variable3 = await client.VariableAsync(user, "test-string", "default");
        var variable4 = await client.VariableAsync(user, "test-number", 9);
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
