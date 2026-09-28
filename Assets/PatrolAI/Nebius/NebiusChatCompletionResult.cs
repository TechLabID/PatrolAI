using System;

namespace PatrolAI.Nebius
{
    /// <summary>
    /// Secret-free result of one Nebius chat-completions request.
    /// </summary>
    [Serializable]
    public sealed class NebiusChatCompletionResult
    {
        public bool success;
        public int statusCode;
        public string message;
        public string modelOutput;
        public long elapsedMilliseconds;
        public string timestamp;
    }
}
