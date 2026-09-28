using System;

/// <summary>
/// Serializable observation contract reserved for future communication between
/// the inherited perception pipeline and the PatrolAI reasoning layer.
/// </summary>
[Serializable]
public class PatrolObservation
{
    public string observationId;
    public string timestamp;
    public string source;
    public string detectedCondition;
    public string evidenceStatus;
    public float confidence;
    public string location;
    public string additionalContext;
}
