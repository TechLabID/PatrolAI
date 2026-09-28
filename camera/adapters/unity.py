from pathlib import Path
from typing import Protocol
import json
from camera.protocol.observation import VisualObservation, ObservationError


class ICameraSource(Protocol):
    def read_observation(self) -> tuple[VisualObservation, Path]: ...


def read_manifest(path: Path) -> tuple[VisualObservation, Path]:
    """JSON is the publication marker: exporter writes it only after the PNG."""
    path = Path(path).resolve()
    try:
        if path.stat().st_size > 65536:
            raise ObservationError('Manifest too large')
        def unique(pairs):
            result = {}
            for key, val in pairs:
                if key in result:
                    raise ObservationError('Duplicate JSON key')
                result[key] = val
            return result
        value = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique)
        observation = VisualObservation.from_dict(value)
        frame = (path.parent / observation.frame).resolve()
        if frame.parent != path.parent:
            raise ObservationError('Frame escapes observation directory')
        return observation, frame
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise ObservationError('Cannot read observation manifest') from exc


class UnityCameraAdapter:
    def __init__(self, manifest: Path):
        self.manifest = Path(manifest)

    def read_observation(self):
        observation, frame = read_manifest(self.manifest)
        if observation.source_type != 'unity':
            raise ObservationError('UnityCameraAdapter requires a Unity observation')
        return observation, frame
