using DevCycle.SDK.Server.Common.Model;
using Dynatrace.OneAgent.Sdk.Api;

namespace HelloTogglebot.Hooks
{
    public class DynatraceSpanHook : EvalHook
    {
        private readonly IOneAgentSdk oneAgentSdk;
        private ThreadLocal<ITracer> currentTracer;

        public DynatraceSpanHook(IOneAgentSdk oneAgentSdk)
        {
            this.oneAgentSdk = oneAgentSdk;
            this.currentTracer = new ThreadLocal<ITracer>();
        }

        public override async Task<HookContext<T>> BeforeAsync<T>(HookContext<T> context, CancellationToken cancellationToken = default)
        {
            // since there are no custom tracers in C# oneAgentSdk, outgoing webrequest is closest
            this.currentTracer.Value = (ITracer)oneAgentSdk.TraceIncomingRemoteCall(
            "VariableAsync",
            "DevCycle.SDK",
            $"feature_flag_evaluation.{context.Key}"
            );

            this.currentTracer.Value.Start();

            oneAgentSdk.AddCustomRequestAttribute("feature_flag.key", context.Key);
            oneAgentSdk.AddCustomRequestAttribute("feature_flag.provider.name", "devcycle");
            oneAgentSdk.AddCustomRequestAttribute("feature_flag.context.id", context.User.UserId);

            if (context.Metadata != null)
            {
                if (context.Metadata.Project?.Id != null)
                {
                    oneAgentSdk.AddCustomRequestAttribute("feature_flag.project", context.Metadata.Project.Id);
                }
                if (context.Metadata.Environment?.Id != null)
                {
                    oneAgentSdk.AddCustomRequestAttribute("feature_flag.environment", context.Metadata.Environment.Id);
                }
            }

            return await Task.FromResult(context);
        }

        public override Task AfterAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {

            oneAgentSdk.AddCustomRequestAttribute("feature_flag.result.reason", variableDetails.Eval.Reason);
            oneAgentSdk.AddCustomRequestAttribute("feature_flag.result.reason.details", variableDetails.Eval.Details);
            if (variableDetails.IsDefaulted == false)
            {
                oneAgentSdk.AddCustomRequestAttribute("feature_flag.set.id", variableMetadata.FeatureId);
                oneAgentSdk.AddCustomRequestAttribute("feature_flag.url", $"https://app.devcycle.com/r/p/{context.Metadata.Project.Id}/f/{variableMetadata.FeatureId}");
            }
            return Task.CompletedTask;
        }

        public override Task ErrorAsync<T>(HookContext<T> context, System.Exception error, CancellationToken cancellationToken = default)
        {
            oneAgentSdk.AddCustomRequestAttribute("feature_flag.error_message", error.Message);
            oneAgentSdk.AddCustomRequestAttribute("error.type", error.GetType().Name);
            return Task.CompletedTask;
        }

        public override Task FinallyAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            this.currentTracer.Value?.End();
            return Task.CompletedTask;
        }
    }
}
