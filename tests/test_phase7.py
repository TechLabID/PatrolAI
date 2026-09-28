from pathlib import Path
import unittest

from tools.run_phase7_evaluation import (
    DEFAULT_SCENARIOS,
    _mean,
    _rate,
    load_scenarios,
)


class Phase7EvaluationTests(unittest.TestCase):
    def test_catalog_has_explicit_separate_ground_truth(self):
        catalog = load_scenarios(DEFAULT_SCENARIOS)
        self.assertEqual(
            catalog['ground_truth_kind'], 'explicit_simulated_ground_truth')
        self.assertEqual(len(catalog['scenarios']), 10)
        for scenario in catalog['scenarios']:
            self.assertEqual(scenario['target_id'], 'SWG_05/Indicator_03')
            self.assertTrue(any(
                key.startswith('expected_') for key in scenario))
            self.assertNotIn('observed', scenario)
            self.assertNotIn('prediction', scenario)

    def test_required_scenarios_and_conflict_control_exist(self):
        identifiers = {
            scenario['id'] for scenario in
            load_scenarios(DEFAULT_SCENARIOS)['scenarios']
        }
        required = {
            'A_clear_red',
            'B_clear_green',
            'C_primary_insufficient_secondary_sufficient',
            'D_multi_source_agreement',
            'E_insufficient_multi_source',
            'F_repeated_unchanged_condition',
            'G_red_to_green_confirmed_change',
            'H_replay_same_incident',
            'I_cloud_unavailable_local_preserved',
            'J_multi_source_conflict',
        }
        self.assertEqual(identifiers, required)

    def test_metric_helpers_are_deterministic(self):
        self.assertEqual(_rate(2, 3), 0.666667)
        self.assertEqual(_rate(0, 0), 0.0)
        self.assertEqual(_mean([1.0, 2.0, 3.0]), 2.0)

    def test_frozen_green_fixture_is_phase7_local(self):
        scenario = next(
            item for item in load_scenarios(DEFAULT_SCENARIOS)['scenarios']
            if item['id'] == 'B_clear_green')
        manifest = Path(__file__).resolve().parents[1] / scenario['fixture']
        self.assertTrue(manifest.is_file())
        self.assertTrue((manifest.parent /
                         'obs_3ccb675e1d66434dba9fc0e41035922c.png').is_file())


if __name__ == '__main__':
    unittest.main()
