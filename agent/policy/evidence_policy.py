from dataclasses import dataclass


@dataclass(frozen=True)
class EvidenceAssessment:
    sufficient: bool
    action: str
    reason: str
    candidate_condition: str


class EvidencePolicy:
    """Explicit policy over measured OpenCV evidence, never a fault diagnosis."""

    accepted_conditions = {'INDICATOR_RED', 'INDICATOR_AMBER', 'INDICATOR_GREEN'}

    def __init__(self, minimum_score=0.6):
        self.minimum_score = minimum_score

    @staticmethod
    def candidate_condition(evidence):
        colors = evidence.get('measurements', {}).get('colors', {})
        areas = {name: value.get('area_pixels', 0) for name, value in colors.items()
                 if name in ('RED', 'AMBER', 'GREEN') and isinstance(value, dict)}
        if not areas or max(areas.values()) <= 0:
            return 'UNKNOWN'
        return 'INDICATOR_' + max(areas, key=areas.get)

    def assess(self, evidence):
        candidate = self.candidate_condition(evidence)
        confidence = evidence.get('confidence')
        sufficient = (
            evidence.get('evidence_quality') == 'SUFFICIENT'
            and evidence.get('condition') in self.accepted_conditions
            and type(confidence) in (float, int)
            and confidence >= self.minimum_score
        )
        if sufficient:
            return EvidenceAssessment(True, 'VERIFY', 'evidence_meets_policy', candidate)
        reasons = []
        if evidence.get('evidence_quality') != 'SUFFICIENT':
            reasons.append('quality_insufficient')
        if evidence.get('condition') not in self.accepted_conditions:
            reasons.append('condition_unresolved')
        if type(confidence) not in (float, int) or confidence < self.minimum_score:
            reasons.append('score_below_policy')
        return EvidenceAssessment(False, 'REQUEST_REOBSERVATION', '+'.join(reasons), candidate)
