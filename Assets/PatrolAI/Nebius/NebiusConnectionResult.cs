using System;

namespace PatrolAI.Nebius
{
    /// <summary>
    /// Secret-free result returned by a Nebius connectivity test.
    /// </summary>
    [Serializable]
    public sealed class NebiusConnectionResult
    {
        public bool success;
        public int statusCode;
        public string message;
        public string responseBody;
        public long elapsedMilliseconds;
        public string timestamp;
    }
}
