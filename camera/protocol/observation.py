from dataclasses import dataclass
from datetime import datetime
import math
import re


class ObservationError(ValueError):
    """Unusable observation; callers must not manufacture visual evidence."""


def require(condition, message):
    if not condition:
        raise ObservationError(message)


@dataclass(frozen=True)
class VisualObservation:
    observation_id: str
    camera_id: str
    timestamp: str
    source_type: str
    frame: str
    frame_sha256: str
    width: int
    height: int
    camera_pose: dict | None
    metadata: dict
    schema_version: int = 1

    @classmethod
    def from_dict(cls, value):
        require(type(value) is dict, 'Observation must be an object')
        require(set(value) == set(cls.__dataclass_fields__), 'Missing or unknown observation fields')
        require(type(value['schema_version']) is int and value['schema_version'] == 1,
                'Unsupported schema_version')
        for key in ('observation_id', 'camera_id'):
            require(type(value[key]) is str and re.fullmatch(r'[A-Za-z0-9_-]{1,80}', value[key]),
                    f'Invalid {key}')
        require(value['source_type'] in ('unity', 'synthetic_fixture'), 'Unsupported source_type')
        try:
            stamp = datetime.fromisoformat(value['timestamp'].replace('Z', '+00:00'))
            require(stamp.utcoffset() is not None, 'Timestamp must include timezone')
        except (ValueError, TypeError, AttributeError) as exc:
            raise ObservationError('Invalid timestamp') from exc
        for key in ('width', 'height'):
            require(type(value[key]) is int and 16 <= value[key] <= 4096, f'Invalid {key}')
        require(type(value['frame']) is str and re.fullmatch(r'[A-Za-z0-9_-]{1,100}\.png', value['frame']),
                'Frame must be a relative PNG basename')
        require(type(value['frame_sha256']) is str and re.fullmatch(r'[a-f0-9]{64}', value['frame_sha256']),
                'Invalid frame_sha256')
        pose = value['camera_pose']
        if pose is not None:
            require(type(pose) is dict and set(pose) == {'position', 'rotation_xyzw'}, 'Invalid pose')
            for key, size in (('position', 3), ('rotation_xyzw', 4)):
                require(type(pose[key]) is list and len(pose[key]) == size, f'Invalid pose {key}')
                require(all(type(n) in (float, int) and math.isfinite(n) for n in pose[key]),
                        'Pose must be finite numbers')
            require(abs(sum(n*n for n in pose['rotation_xyzw'])-1) < .001, 'Rotation must be unit quaternion')
        meta = value['metadata']
        require(type(meta) is dict and set(meta) == {'target_id', 'roi', 'provenance'}, 'Invalid metadata')
        require(type(meta['target_id']) is str and re.fullmatch(r'[A-Za-z0-9_/-]{1,100}', meta['target_id']),
                'Invalid target_id')
        require(meta['provenance'] == ('unity_render' if value['source_type'] == 'unity' else 'synthetic_test_only'),
                'Source and provenance disagree')
        roi = meta['roi']
        require(type(roi) is list and len(roi) == 4 and all(type(n) is int for n in roi), 'Invalid ROI')
        x, y, w, h = roi
        require(x >= 0 and y >= 0 and w >= 8 and h >= 8 and x+w <= value['width'] and y+h <= value['height'],
                'ROI outside frame or too small')
        return cls(**value)
