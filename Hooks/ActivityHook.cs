using System.Diagnostics;
using System.Collections.Concurrent;
using DevCycle.SDK.Server.Common.Model;

namespace HelloTogglebot.Hooks
{
    public class ActivityHook : EvalHook
    {
        private readonly ActivitySource _activitySource;
        private readonly ConcurrentDictionary<string, Activity> _activities = new();

        public ActivityHook(ActivitySource activitySource)
        {
            _activitySource = activitySource;
        }

        public override async Task<HookContext<T>> BeforeAsync<T>(HookContext<T> context, CancellationToken cancellationToken = default)
        {
            var activity = _activitySource.StartActivity($"feature_flag_evaluation.{context.Key}");

            if (activity != null)
            {
                var activityKey = $"{context.Key}_{context.User.UserId}";

                activity.SetTag("feature_flag.key", context.Key);
                activity.SetTag("feature_flag.provider.name", "devcycle");
                activity.SetTag("feature_flag.context.id", context.User.UserId);
                activity.SetTag("feature_flag.value_type", context.DefaultValue.GetType().Name);

                if (context.Metadata != null)
                {
                    activity.SetTag("feature_flag.project", context.Metadata.Project?.Id);
                    activity.SetTag("feature_flag.environment", context.Metadata.Environment?.Id);
                }

                _activities.TryAdd(activityKey, activity);
            }

            return await Task.FromResult(context);
        }

        public override Task AfterAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var activityKey = $"{context.Key}_{context.User.UserId}";
            if (_activities.TryGetValue(activityKey, out var activity))
            {
                activity.SetTag("feature_flag.result.value", variableDetails.Value.ToString());
                if (variableMetadata.FeatureId != null)
                {
                    activity.SetTag("feature_flag.set.id", variableMetadata.FeatureId);
                    activity.SetTag("feature_flag.url", $"https://app.devcycle.com/r/p/{context.Metadata.Project.Id}/f/{variableMetadata.FeatureId}");
                }
                if (variableDetails.Eval != null)
                {
                    activity.SetTag("feature_flag.result.reason", variableDetails.Eval.Reason);
                    activity.SetTag("feature_flag.result.reason.details", variableDetails.Eval.Details);
                }
            }
            return Task.CompletedTask;
        }

        public override Task ErrorAsync<T>(HookContext<T> context, System.Exception error, CancellationToken cancellationToken = default)
        {
            var activityKey = $"{context.Key}_{context.User.UserId}";
            if (_activities.TryGetValue(activityKey, out var activity))
            {
                activity.SetTag("feature_flag.error_message", error.Message);
                activity.SetTag("error.type", error.GetType().Name);
            }
            return Task.CompletedTask;
        }

        public override Task FinallyAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var activityKey = $"{context.Key}_{context.User.UserId}";
            if (_activities.TryRemove(activityKey, out var activity))
            {
                activity.Stop();
            }
            return Task.CompletedTask;
        }
    }
}
