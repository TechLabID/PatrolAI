using System;

/// <summary>
/// Serializable result contract reserved for future PatrolAI risk reasoning.
/// Expected riskLevel values: SAFE, WARNING, DANGER, UNKNOWN.
/// Expected action values: CONTINUE, REOBSERVE, INSPECT, STOP, ALERT, REROUTE.
/// </summary>
[Serializable]
public class PatrolDecision
{
    public string decisionId;
    public string riskLevel;
    public string action;
    public string reason;
    public bool requiresReobservation;
    public float confidence;
}
