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
            var attributes = new Dictionary<string, object>
            {
                ["feature_flag.key"] = context.Key,
                ["feature_flag.provider.name"] = "devcycle",
                ["feature_flag.context.id"] = context.User.UserId,
                ["feature_flag.value_type"] = context.DefaultValue.GetType().Name,
                ["feature_flag.error_message"] = error.Message,
                ["error.type"] = error.GetType().Name,
            };

            if (context.Metadata != null)
            {
                attributes["feature_flag.project"] = context.Metadata.Project?.Id;
                attributes["feature_flag.environment"] = context.Metadata.Environment?.Id;
            }
            using (_logger.BeginScope(attributes))
            {
                _logger.LogError($"Error evaluating: {context.Key}");
            }
            return Task.CompletedTask;
        }

        public override Task FinallyAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var attributes = new Dictionary<string, object>
            {
                ["feature_flag.key"] = context.Key,
                ["feature_flag.provider.name"] = "devcycle",
                ["feature_flag.context.id"] = context.User.UserId,
                ["feature_flag.value_type"] = context.DefaultValue.GetType().Name,
                ["feature_flag.result.value"] = variableDetails.Value?.ToString()
            };

            // Add metadata if available
            if (context.Metadata != null)
            {
                attributes["feature_flag.project"] = context.Metadata.Project?.Id;
                attributes["feature_flag.environment"] = context.Metadata.Environment?.Id;
            }

            // Add feature-specific attributes if available
            if (variableMetadata.FeatureId != null)
            {
                attributes["feature_flag.set.id"] = variableMetadata.FeatureId;
                attributes["feature_flag.url"] = $"https://app.devcycle.com/r/p/{context.Metadata.Project.Id}/f/{variableMetadata.FeatureId}";
            }

            // Add evaluation details if available
            if (variableDetails.Eval != null)
            {
                attributes["feature_flag.result.reason"] = variableDetails.Eval.Reason;
                attributes["feature_flag.result.reason.details"] = variableDetails.Eval.Details;
            }

            using (_logger.BeginScope(attributes))
            {
                _logger.LogInformation($"Feature flag evaluated {context.Key}");
            }
            return Task.CompletedTask;
        }
    }
}
