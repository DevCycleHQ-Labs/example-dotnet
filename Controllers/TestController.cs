using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using OpenTelemetry.Trace;

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
    public IActionResult TestTrace()
    {
        using var activity = ActivitySource.StartActivity("TestController.TestTrace");

        _logger.LogInformation("Test trace endpoint called - Activity: {ActivityId}", activity?.Id);

        activity?.SetTag("test.endpoint", "trace");
        activity?.SetTag("test.timestamp", DateTimeOffset.UtcNow.ToString());

        return Ok(new
        {
            message = "Test trace created",
            activityId = activity?.Id,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("flush")]
    public IActionResult FlushTraces([FromServices] TracerProvider? tracerProvider)
    {
        _logger.LogInformation("Manually flushing traces...");

        var result = tracerProvider?.ForceFlush(5000);

        return Ok(new
        {
            message = "Traces flushed",
            success = result,
            timestamp = DateTimeOffset.UtcNow
        });
    }

}
