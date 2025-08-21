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
                // Create key for this evaluation
                var contextKey = $"{context.Key}_{context.User.UserId}";
                _activities[contextKey] = activity;

                Console.WriteLine($"BeforeAsync: created activity {activity.Id} with key {contextKey}");

                activity.SetTag("feature_flag.key", context.Key);
                activity.SetTag("feature_flag.provider.name", "devcycle");
                activity.SetTag("feature_flag.context.id", context.User.UserId);

                if (context.Metadata != null)
                {
                    activity.SetTag("feature_flag.project", context.Metadata.Project?.Id);
                    activity.SetTag("feature_flag.environment", context.Metadata.Environment?.Id);
                }
            }

            return await Task.FromResult(context);
        }

        public override Task AfterAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var contextKey = $"{context.Key}_{context.User.UserId}";
            if (_activities.TryGetValue(contextKey, out var activity))
            {
                Console.WriteLine($"AfterAsync: using stored activity {activity.Id} for key {contextKey}");

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
            else
            {
                Console.WriteLine($"AfterAsync: could not find stored activity for key {contextKey}, falling back to Activity.Current {Activity.Current?.Id}");
            }
            return Task.CompletedTask;
        }

        public override Task ErrorAsync<T>(HookContext<T> context, System.Exception error, CancellationToken cancellationToken = default)
        {
            var contextKey = $"{context.Key}_{context.User.UserId}";
            if (_activities.TryGetValue(contextKey, out var activity))
            {
                Console.WriteLine($"ErrorAsync: using stored activity {activity.Id} for key {contextKey}");
                activity.SetTag("feature_flag.error_message", error.Message);
                activity.SetTag("error.type", error.GetType().Name);
            }
            else
            {
                Console.WriteLine($"ErrorAsync: could not find stored activity for key {contextKey}, falling back to Activity.Current {Activity.Current?.Id}");
                Activity.Current?.SetTag("feature_flag.error_message", error.Message);
                Activity.Current?.SetTag("error.type", error.GetType().Name);
            }
            return Task.CompletedTask;
        }

        public override Task FinallyAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var contextKey = $"{context.Key}_{context.User.UserId}";
            if (_activities.TryRemove(contextKey, out var activity))
            {
                Console.WriteLine($"FinallyAsync: stopping stored activity {activity.Id} for key {contextKey}");
                activity.Stop();
            }
            else
            {
                Console.WriteLine($"FinallyAsync: could not find stored activity for key {contextKey}");
            }
            return Task.CompletedTask;
        }
    }
}
