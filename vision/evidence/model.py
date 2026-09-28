from dataclasses import asdict, dataclass


@dataclass(frozen=True)
class VisualEvidence:
    observation_id: str
    camera_id: str
    target_id: str
    timestamp: str
    condition: str
    confidence: float
    roi: list[int]
    measurements: dict
    evidence_quality: str
    source_type: str
    frame_sha256: str
    opencv_version: str
    schema_version: int = 1
    method: str = 'hsv_indicator_v1'
    confidence_kind: str = 'deterministic_visual_score_not_probability'
    domain: str = 'simulated_visual_condition'

    def to_dict(self):
        return asdict(self)
