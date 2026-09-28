using System;

namespace PatrolAI.Nebius
{
    /// <summary>
    /// Configuration for the isolated Nebius Token Factory service layer.
    /// Credentials are resolved from the configured environment variable only.
    /// </summary>
    [Serializable]
    public sealed class NebiusConfig
    {
        public const string DefaultBaseUrl = "https://api.tokenfactory.nebius.com";
        public const string DefaultApiKeyEnvironmentVariable = "NEBIUS_API_KEY";
        public const int DefaultTimeoutSeconds = 30;

        public string baseUrl = DefaultBaseUrl;
        public string apiKeyEnvironmentVariable = DefaultApiKeyEnvironmentVariable;
        public int timeoutSeconds = DefaultTimeoutSeconds;
        public string modelId = string.Empty;

        public bool TryGetApiKey(out string apiKey, out string errorMessage)
        {
            apiKey = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(apiKeyEnvironmentVariable))
            {
                errorMessage = "Nebius API key environment-variable name is not configured.";
                return false;
            }

            apiKey = Environment.GetEnvironmentVariable(apiKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = string.Empty;
                errorMessage = "Nebius API key is unavailable. Set the "
                    + apiKeyEnvironmentVariable
                    + " environment variable and restart the calling process.";
                return false;
            }

            return true;
        }

        public int GetEffectiveTimeoutSeconds()
        {
            return timeoutSeconds > 0 ? timeoutSeconds : DefaultTimeoutSeconds;
        }
    }
}
