using DevCycle.SDK.Server.Common.Model;

namespace HelloTogglebot.Hooks
{
    public class LogHook : EvalHook
    {
        private readonly ILogger _logger;

        public LogHook(ILogger logger)
        {
            _logger = logger;
        }

        public override Task ErrorAsync<T>(HookContext<T> context, System.Exception error, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Feature flag error");
            return Task.CompletedTask;
        }

        public override Task FinallyAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"feature flag evaluated key:{variableDetails.Key}, value: {variableDetails.Value}");
            return Task.CompletedTask;
        }
    }
}
