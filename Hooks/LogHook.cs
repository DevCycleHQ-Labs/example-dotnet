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
            var attributes = SetContextAttributes<T>(context);
            attributes.Add("feature_flag.error_message", error.Message);
            attributes.Add("error.type", error.GetType().Name);

            using (_logger.BeginScope(attributes))
            {
                _logger.LogError($"Error evaluating flag: {context.Key}");
            }
            return Task.CompletedTask;
        }

        public override Task AfterAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var attributes = SetContextAttributes<T>(context);
            attributes.Add("feature_flag.result.value", variableDetails.Value.ToString() ?? "");
            if (variableMetadata.FeatureId != null)
            {
                attributes.Add("feature_flag.set.id", variableMetadata.FeatureId);
                attributes.Add("feature_flag.url", $"https://app.devcycle.com/r/p/{context.Metadata?.Project.Id}/f/{variableMetadata.FeatureId}");
            }
            if (variableDetails.Eval != null)
            {
                attributes.Add("feature_flag.result.reason", variableDetails.Eval.Reason);
                attributes.Add("feature_flag.result.reason.details", variableDetails.Eval.Details);
            }

            using (_logger.BeginScope(attributes))
            {
                _logger.LogInformation($"Feature flag evaluated {context.Key}");
            }
            return Task.CompletedTask;
        }

        private IDictionary<string, object> SetContextAttributes<T>(HookContext<T> context)
        {
            var attributes = new Dictionary<string, object>
            {
                ["feature_flag.key"] = context.Key,
                ["feature_flag.provider.name"] = "devcycle",
                ["feature_flag.context.id"] = context.User.UserId,
            };
            if (context.DefaultValue != null)
            {
                attributes.Add("feature_flag.value_type", context.DefaultValue.GetType().Name);
            }
            if (context.Metadata != null)
            {
                attributes.Add("feature_flag.project", context.Metadata.Project.Id);
                attributes.Add("feature_flag.environment", context.Metadata.Environment.Id);
            }
            return attributes;
        }
    }
}
