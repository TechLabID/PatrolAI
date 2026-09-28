from agent.policy.evidence_policy import EvidencePolicy


class FusionError(ValueError):
    """The supplied records cannot form a trustworthy fusion input."""


class EvidenceFusionPolicy:
    """Deterministic fusion over existing VisualEvidence records."""

    allowed_sources = {'VG_CCTV_01', 'VG_CCTV_02', 'VG_MOBILE_01'}

    def __init__(self, minimum_score=0.6):
        self.evidence_policy = EvidencePolicy(minimum_score)

    def fuse(self, evidence_records):
        if not isinstance(evidence_records, (list, tuple)) or not evidence_records:
            raise FusionError('at_least_one_evidence_record_required')

        provenance = []
        sufficient = []
        sources = set()
        target_id = None

        for evidence in evidence_records:
            self._validate_record(evidence)
            source = evidence['camera_id']
            if source in sources:
                raise FusionError('duplicate_source_id:' + source)
            sources.add(source)

            if target_id is None:
                target_id = evidence['target_id']
            elif evidence['target_id'] != target_id:
                raise FusionError('evidence_target_mismatch')

            record = {
                'camera_id': source,
                'observation_id': evidence['observation_id'],
                'condition': evidence['condition'],
                'quality': evidence['evidence_quality'],
                'score': evidence['confidence'],
            }
            if 'timestamp' in evidence:
                record['timestamp'] = evidence['timestamp']
            provenance.append(record)

            if self.evidence_policy.assess(evidence).sufficient:
                sufficient.append(evidence)

        base = {
            'schema_version': 1,
            'target_id': target_id,
            'source_count': len(sources),
            'sufficient_source_count': len(sufficient),
            'provenance': provenance,
        }

        if len(sufficient) < 2:
            return {
                **base,
                'fusion_status': 'INSUFFICIENT',
                'verification': 'UNRESOLVED',
                'action': 'REQUEST_ADDITIONAL_EVIDENCE',
                'final_condition': 'UNKNOWN',
                'reason': 'fewer_than_two_sufficient_sources',
            }

        conditions = {item['condition'] for item in sufficient}
        if len(conditions) != 1:
            return {
                **base,
                'fusion_status': 'CONFLICT',
                'verification': 'UNRESOLVED',
                'action': 'REQUEST_ADDITIONAL_EVIDENCE',
                'final_condition': 'UNKNOWN',
                'reason': 'sufficient_sources_disagree',
            }

        return {
            **base,
            'fusion_status': 'CONSISTENT',
            'verification': 'VERIFIED',
            'action': 'RECORD',
            'final_condition': next(iter(conditions)),
            'reason': 'multiple_sufficient_sources_agree',
        }

    def _validate_record(self, evidence):
        if not isinstance(evidence, dict):
            raise FusionError('evidence_record_must_be_an_object')
        for field in ('camera_id', 'observation_id', 'target_id', 'condition',
                      'evidence_quality'):
            if not isinstance(evidence.get(field), str) or not evidence[field]:
                raise FusionError('invalid_or_missing_' + field)
        if evidence['camera_id'] not in self.allowed_sources:
            raise FusionError('unsupported_source_id:' + evidence['camera_id'])
        if type(evidence.get('confidence')) not in (float, int):
            raise FusionError('invalid_or_missing_confidence')
        if 'timestamp' in evidence and not isinstance(evidence['timestamp'], str):
            raise FusionError('invalid_timestamp')
