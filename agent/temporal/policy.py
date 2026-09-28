"""Deterministic temporal reasoning over existing Phase 4 fusion results."""
import copy
import hashlib


class TemporalError(ValueError):
    """The supplied fusion result or temporal state is invalid."""


class TemporalPolicy:
    allowed_conditions = {
        'INDICATOR_RED',
        'INDICATOR_AMBER',
        'INDICATOR_GREEN',
    }

    def evaluate(self, fusion, state=None):
        self._validate_fusion(fusion)
        target_id = fusion['target_id']
        fusion_id = fusion['session_id']
        observed_at = self._event_time(fusion)
        existing = copy.deepcopy(state) if state is not None else None
        if existing is not None and existing.get('target_id') != target_id:
            raise TemporalError('temporal_state_target_mismatch')

        if self._is_verified(fusion):
            condition = fusion['final_condition']
            if observed_at is None:
                raise TemporalError('verified_fusion_timestamp_required')
            if existing is None or not existing.get('current_verified_condition'):
                new_state = self._baseline_state(
                    target_id, condition, fusion_id, observed_at)
                return self._result(
                    fusion, 'NO_CHANGE', 'verified_baseline_established',
                    new_state, None)

            previous = existing['current_verified_condition']
            if condition == previous:
                new_state = copy.deepcopy(existing)
                new_state.update(
                    previous_verified_condition=previous,
                    current_verified_condition=condition,
                    previous_fusion_id=existing.get('current_fusion_id'),
                    current_fusion_id=fusion_id,
                    last_verified_at=observed_at,
                    change_detected=False,
                    change_id=None,
                    pending_change=None,
                )
                return self._result(
                    fusion, 'NO_CHANGE', 'verified_condition_unchanged',
                    new_state, None)

            pending = existing.get('pending_change') or {}
            if pending.get('candidate_condition') == condition:
                change_id = pending['change_id']
            else:
                change_id = self._id(
                    'chg', target_id, previous, condition, fusion_id)
            incident = self._incident(
                fusion, existing, previous, condition, change_id, observed_at)
            new_state = copy.deepcopy(existing)
            new_state.update(
                previous_verified_condition=previous,
                current_verified_condition=condition,
                previous_fusion_id=existing.get('current_fusion_id'),
                current_fusion_id=fusion_id,
                last_verified_at=observed_at,
                change_detected=True,
                change_id=change_id,
                pending_change=None,
            )
            return self._result(
                fusion, 'CONFIRMED_CHANGE', 'verified_condition_changed',
                new_state, incident)

        if existing is None or not existing.get('current_verified_condition'):
            return self._result(
                fusion, 'UNRESOLVED', 'verified_baseline_required',
                existing, None)

        candidate = self._candidate_condition(fusion)
        previous = existing['current_verified_condition']
        if candidate is not None and candidate != previous:
            change_id = self._id(
                'chg', target_id, previous, candidate, fusion_id)
            new_state = copy.deepcopy(existing)
            new_state.update(
                change_detected=False,
                change_id=change_id,
                pending_change={
                    'change_id': change_id,
                    'candidate_condition': candidate,
                    'candidate_fusion_id': fusion_id,
                    'first_seen_at': observed_at,
                },
            )
            return self._result(
                fusion, 'CHANGE_CANDIDATE',
                'different_condition_not_yet_verified', new_state, None,
                candidate_condition=candidate)

        new_state = copy.deepcopy(existing)
        new_state['change_detected'] = False
        return self._result(
            fusion, 'UNRESOLVED', 'fusion_not_verified', new_state, None)

    def _baseline_state(self, target_id, condition, fusion_id, observed_at):
        return {
            'schema_version': 1,
            'target_id': target_id,
            'previous_verified_condition': None,
            'current_verified_condition': condition,
            'previous_fusion_id': None,
            'current_fusion_id': fusion_id,
            'first_seen_at': observed_at,
            'last_verified_at': observed_at,
            'change_detected': False,
            'change_id': None,
            'pending_change': None,
        }

    def _incident(self, fusion, state, previous, condition, change_id, observed_at):
        supporting = [
            item for item in fusion['provenance']
            if item.get('quality') == 'SUFFICIENT'
            and item.get('condition') == condition
        ]
        incident_id = self._id('inc', change_id, fusion['session_id'])
        return {
            'schema_version': 1,
            'incident_id': incident_id,
            'target_id': fusion['target_id'],
            'previous_condition': previous,
            'new_condition': condition,
            'change_id': change_id,
            'supporting_fusion_ids': [fusion['session_id']],
            'supporting_observation_ids': [
                item['observation_id'] for item in supporting
            ],
            'source_ids': [item['camera_id'] for item in supporting],
            'verification_status': fusion['verification'],
            'previous_verified_at': state.get('last_verified_at'),
            'confirmed_at': observed_at,
        }

    def _result(self, fusion, outcome, reason, state, incident,
                candidate_condition=None):
        return {
            'schema_version': 1,
            'target_id': fusion['target_id'],
            'fusion_id': fusion['session_id'],
            'outcome': outcome,
            'reason': reason,
            'candidate_condition': candidate_condition,
            'previous_verified_condition': (
                state.get('previous_verified_condition') if state else None),
            'current_verified_condition': (
                state.get('current_verified_condition') if state else None),
            'change_detected': bool(state and state.get('change_detected')),
            'change_id': state.get('change_id') if state else None,
            'incident_id': incident['incident_id'] if incident else None,
            'supporting_observation_ids': [
                item['observation_id'] for item in fusion['provenance']
            ],
            'source_ids': [item['camera_id'] for item in fusion['provenance']],
            'state': state,
            'incident': incident,
        }

    def _candidate_condition(self, fusion):
        conditions = {
            item['condition'] for item in fusion['provenance']
            if item.get('quality') == 'SUFFICIENT'
            and item.get('condition') in self.allowed_conditions
            and type(item.get('score')) in (int, float)
            and item['score'] >= 0.6
        }
        return next(iter(conditions)) if len(conditions) == 1 else None

    def _is_verified(self, fusion):
        return (
            fusion['fusion_status'] == 'CONSISTENT'
            and fusion['verification'] == 'VERIFIED'
            and fusion['final_condition'] in self.allowed_conditions
        )

    def _event_time(self, fusion):
        timestamps = [
            item.get('timestamp') for item in fusion['provenance']
            if isinstance(item.get('timestamp'), str) and item['timestamp']
        ]
        return max(timestamps) if timestamps else None

    def _validate_fusion(self, fusion):
        if not isinstance(fusion, dict):
            raise TemporalError('fusion_result_must_be_an_object')
        for field in ('target_id', 'session_id', 'fusion_status',
                      'verification', 'final_condition'):
            if not isinstance(fusion.get(field), str) or not fusion[field]:
                raise TemporalError('invalid_or_missing_' + field)
        provenance = fusion.get('provenance')
        if not isinstance(provenance, list) or not provenance:
            raise TemporalError('fusion_provenance_required')
        for item in provenance:
            if not isinstance(item, dict):
                raise TemporalError('invalid_provenance_record')
            for field in ('camera_id', 'observation_id', 'condition', 'quality'):
                if not isinstance(item.get(field), str) or not item[field]:
                    raise TemporalError('invalid_provenance_' + field)

    @staticmethod
    def _id(prefix, *parts):
        value = '|'.join(str(part) for part in parts)
        digest = hashlib.sha256(value.encode('utf-8')).hexdigest()[:24]
        return prefix + '_' + digest
