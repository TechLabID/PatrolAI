from agent.policy.evidence_policy import EvidencePolicy
from agent.reasoning.verification import verify_evidence


class ActiveInspectionStateMachine:
    def __init__(self, policy=None):
        self.policy = policy or EvidencePolicy()

    def assess_primary(self, evidence):
        assessment = self.policy.assess(evidence)
        path = ['IDLE', 'OBSERVE_PRIMARY', 'ASSESS_PRIMARY']
        if assessment.sufficient:
            path += ['VERIFY', 'COMPLETE']
            result = {
                'target_id': evidence['target_id'],
                'primary_observation': evidence['observation_id'],
                'secondary_observation': '',
                'primary_quality': evidence['evidence_quality'],
                'secondary_quality': 'NOT_REQUESTED',
                'primary_candidate': assessment.candidate_condition,
                'final_condition': evidence['condition'],
                'verification': 'VERIFIED',
                'reason': 'primary_evidence_sufficient',
            }
            return {'state': 'COMPLETE', 'action': 'RECORD', 'reason': assessment.reason,
                    'candidate_condition': assessment.candidate_condition,
                    'state_path': path, 'result': result}
        path.append('REQUEST_REOBSERVATION')
        return {'state': 'REQUEST_REOBSERVATION', 'action': 'MOVE_TO_INSPECTION_POSE',
                'reason': assessment.reason, 'candidate_condition': assessment.candidate_condition,
                'state_path': path, 'result': None}

    def assess_secondary(self, primary, secondary=None, failure_reason=None):
        result = verify_evidence(primary, secondary, failure_reason, self.policy)
        return {'state': 'COMPLETE', 'action': 'RECORD', 'reason': result['reason'],
                'candidate_condition': result['final_condition'],
                'state_path': ['MOVE_INSPECTOR', 'OBSERVE_SECONDARY', 'ASSESS_SECONDARY',
                               'VERIFY', 'COMPLETE'], 'result': result}
