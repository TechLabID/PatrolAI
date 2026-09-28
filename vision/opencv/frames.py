import hashlib
import struct
from pathlib import Path
import cv2
import numpy as np
from camera.protocol.observation import ObservationError


def verify_version():
    if cv2.__version__.split('.')[0] != '5':
        raise ObservationError(f'OpenCV 5 required; found {cv2.__version__}. No fallback allowed.')
    return cv2.__version__


def load_frame(observation, path: Path):
    verify_version()
    try:
        if path.stat().st_size > 32 * 1024 * 1024:
            raise ObservationError('Frame exceeds 32 MiB limit')
        data = path.read_bytes()
    except OSError as exc:
        raise ObservationError('Missing or unreadable frame') from exc
    if hashlib.sha256(data).hexdigest() != observation.frame_sha256:
        raise ObservationError('Frame SHA256 mismatch')
    if len(data) < 33 or data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR':
        raise ObservationError('Not a PNG image')
    if struct.unpack('>II', data[16:24]) != (observation.width, observation.height):
        raise ObservationError('PNG dimensions disagree with manifest')
    try:
        frame = cv2.imdecode(np.frombuffer(data, np.uint8), cv2.IMREAD_COLOR)
    except cv2.error as exc:
        raise ObservationError('OpenCV could not decode frame') from exc
    if frame is None or frame.shape != (observation.height, observation.width, 3):
        raise ObservationError('Corrupted or unsupported image')
    return frame
