using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using OpenTelemetry.Trace;
using Dynatrace.OneAgent.Sdk.Api;
using DevCycle.SDK.Server.Common.Model;

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
    public async Task<IActionResult> TestTraceAsync()
    {

        var client = DevCycleClient.GetClient();
        var user = new DevCycleUser("userId");
        var variable = await client.VariableAsync(user, "test", false);

        return Ok(new
        {
            message = "Test trace created",
            timestamp = DateTimeOffset.UtcNow,
            variable = variable
        });
    }

    [HttpGet("oneagent")]
    public async Task<IActionResult> TestOneAgentAsync([FromServices] IOneAgentSdk oneAgent)
    {
        _logger.LogInformation("OneAgent SDK test endpoint called - State: {State}", oneAgent.CurrentState);

        var client = DevCycleClient.GetClient();
        var user = new DevCycleUser("userId");
        var variable = await client.VariableAsync(user, "test", false);

        return Ok(new
        {
            message = "OneAgent SDK available",
            sdkState = oneAgent.CurrentState.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            variable = variable
        });
    }

}
