import cv2
import numpy as np
from vision.opencv.frames import verify_version
from vision.evidence.model import VisualEvidence


def analyze_indicator(observation, frame):
    version = verify_version()
    x, y, w, h = observation.metadata['roi']
    if frame.dtype != np.uint8 or frame.shape != (observation.height, observation.width, 3):
        raise ValueError('Expected manifest-sized uint8 BGR frame')
    hsv = cv2.cvtColor(frame[y:y+h, x:x+w], cv2.COLOR_BGR2HSV)
    ranges = {'RED': [(0, 10), (170, 179)], 'AMBER': [(11, 35)], 'GREEN': [(36, 89)]}
    kernel = np.ones((3, 3), np.uint8)
    measurements = {}
    for color, intervals in ranges.items():
        mask = np.zeros((h, w), np.uint8)
        for low, high in intervals:
            mask = cv2.bitwise_or(mask, cv2.inRange(hsv, (low, 90, 65), (high, 255, 255)))
        mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
        count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
        area = 0; fill = 0.; aspect = 0.; center_score = 0.; clipped = False
        if count > 1:
            index = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
            left, top, cw, ch, area = (int(v) for v in stats[index])
            fill = area / (cw * ch)
            aspect = min(cw, ch) / max(cw, ch)
            center_score = max(0., 1. - ((left+cw/2-w/2)**2+(top+ch/2-h/2)**2)**.5 / (min(w,h)/2))
            clipped = left == 0 or top == 0 or left+cw == w or top+ch == h
        measurements[color] = dict(area_pixels=area, area_fraction=round(area/(w*h), 6),
                                   fill_ratio=round(fill, 6), aspect_ratio=round(aspect, 6),
                                   center_score=round(center_score, 6), touches_roi_border=clipped)
    ranked = sorted(measurements, key=lambda key: measurements[key]['area_pixels'], reverse=True)
    winner = ranked[0]; best = measurements[winner]
    dominance = best['area_pixels'] / max(1, sum(v['area_pixels'] for v in measurements.values()))
    # Defined score: size support * dominant color share * near-circular shape * ROI centering.
    size_support = min(1., best['area_fraction']/.12)
    shape_support = best['aspect_ratio'] * min(1., best['fill_ratio']/.65)
    score = round(size_support * dominance * shape_support * best['center_score'], 6)
    sufficient = (best['area_pixels'] >= 16 and .025 <= best['area_fraction'] <= .65
                  and dominance >= .8 and best['aspect_ratio'] >= .65
                  and not best['touches_roi_border'] and score >= .6)
    return VisualEvidence(observation.observation_id, observation.camera_id, observation.metadata['target_id'],
                          observation.timestamp, 'INDICATOR_'+winner if sufficient else 'UNKNOWN',
                          score if sufficient else 0., list(observation.metadata['roi']),
                          dict(colors=measurements, dominance=round(dominance, 6),
                               candidate_score=score, score_threshold=.6, minimum_component_pixels=16),
                          'SUFFICIENT' if sufficient else 'INSUFFICIENT', observation.source_type,
                          observation.frame_sha256, version)
