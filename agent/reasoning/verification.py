from agent.policy.evidence_policy import EvidencePolicy


def verify_evidence(primary, secondary=None, failure_reason=None, policy=None):
    policy = policy or EvidencePolicy()
    primary_assessment = policy.assess(primary)
    base = {
        'target_id': primary.get('target_id', ''),
        'primary_observation': primary.get('observation_id', ''),
        'secondary_observation': secondary.get('observation_id', '') if secondary else '',
        'primary_quality': primary.get('evidence_quality', 'INSUFFICIENT'),
        'secondary_quality': secondary.get('evidence_quality', 'FAILED') if secondary else 'FAILED',
        'primary_candidate': primary_assessment.candidate_condition,
        'final_condition': 'UNKNOWN',
        'verification': 'UNRESOLVED',
        'reason': failure_reason or '',
    }
    if failure_reason or secondary is None:
        base['reason'] = failure_reason or 'secondary_evidence_missing'
        return base
    secondary_assessment = policy.assess(secondary)
    if not secondary_assessment.sufficient:
        base['reason'] = 'secondary_evidence_insufficient'
        return base
    base['final_condition'] = secondary['condition']
    if primary_assessment.sufficient:
        if primary['condition'] == secondary['condition']:
            base.update(verification='VERIFIED', reason='two_sufficient_views_agree')
        else:
            base.update(verification='REJECTED', reason='two_sufficient_views_disagree')
        return base
    if primary_assessment.candidate_condition == secondary['condition']:
        base.update(verification='VERIFIED', reason='secondary_sufficient_confirms_primary_candidate')
    else:
        base['reason'] = 'insufficient_primary_disagrees_with_secondary'
    return base
