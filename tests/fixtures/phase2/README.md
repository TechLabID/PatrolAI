# Perception fixtures

`synthetic_indicators.png` is an original, deterministic OpenCV drawing of three colored lamps on a generic panel. **It is NOT a Unity screenshot, RF90 rendering, or proof of Unity integration.** The manifest explicitly uses `source_type=synthetic_fixture`, `provenance=synthetic_test_only`. The production Unity adapter rejects it.

Known fixture ROI `[211,41,48,48]` contains the red lamp. Neighboring ROIs `[61,41,48,48]` and `[136,41,48,48]` contain green and amber. Tests exercise all three and verify UNKNOWN for blank, clipped, ambiguous or solid-color input. `expected_red_evidence.json` is the pinned deterministic regression output under OpenCV 5.0.0, not electrical ground truth.

Generate the synthetic input only with `.venv-phase2\Scripts\python.exe tools\make_phase2_fixture.py`. Do not update the expected evidence merely to make a failing test pass; inspect the changed measurements first.

After the first manual Unity capture, run `.venv-phase2\Scripts\python.exe tools\process_latest_observation.py --save-fixture`. This preserves the actual PNG, original manifest, and measured evidence under `unity/<observation_id>/`, refusing to overwrite existing fixtures. Replay with `python -m vision.opencv.run tests/fixtures/phase2/unity/<observation_id>/observation.json --annotated`. Human inspection of the actual lamp/ROI is still needed to establish a Unity expected-label regression.
