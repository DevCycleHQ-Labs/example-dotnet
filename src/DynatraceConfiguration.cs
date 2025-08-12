namespace HelloTogglebot
{
    using System;

    public static class DynatraceConfiguration
    {
        public static string? Endpoint => Environment.GetEnvironmentVariable("DYNATRACE_ENDPOINT");
        public static string? ApiToken => Environment.GetEnvironmentVariable("DYNATRACE_API_TOKEN");
        public static string ServiceName => Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "HelloTogglebot";
        public static string ServiceVersion => Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "1.0.0";

        public static bool IsConfigured => !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(ApiToken);

        public static string GetOtlpEndpoint()
        {
            if (string.IsNullOrEmpty(Endpoint))
                throw new InvalidOperationException("DYNATRACE_ENDPOINT environment variable is not set");

            return $"{Endpoint.TrimEnd('/')}/api/v2/otlp/v1/traces";
        }

        public static Dictionary<string, string> GetHeaders()
        {
            if (string.IsNullOrEmpty(ApiToken))
                throw new InvalidOperationException("DYNATRACE_API_TOKEN environment variable is not set");

            return new Dictionary<string, string>
            {
                ["Authorization"] = $"Api-Token {ApiToken}"
            };
        }
    }
}