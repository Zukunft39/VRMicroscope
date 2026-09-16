"""English-output regression checks using the offline integration path."""
import json
from pathlib import Path
import re
import unittest
from unittest.mock import patch

import assistant_chat as chat
import guidance
import navigation
import speech
from test_guidance import state, request
from test_navigation import snapshot


class EnglishLocale(unittest.TestCase):
    def assert_english(self, text):
        self.assertIsNone(re.search(r'[\u3400-\u9fff]', text))

    def test_loaded_knowledge_and_allowed_prose_are_english(self):
        bundle = chat.KnowledgeBundle()
        self.assert_english(bundle.rules + bundle.facts + guidance.RULES + navigation.RULES)
        self.assert_english(guidance.CATALOG_TEXT)
        for action in guidance.ACTIONS.values():
            for device in ('desktop', 'xr'):
                rendered = 'After closing the chat window, ' + action[device] + '\nObserve: ' + action['observation']
                self.assertLessEqual(len(rendered), 650, action['id'])

    def test_english_topic_questions_produce_english_mock_answers(self):
        cases = [('How does NA differ from magnification?', 'numerical_aperture'),
                 ('What does the spatial frequency module show?', 'spatial_frequency'),
                 ('Why does a confocal pinhole suppress defocus?', 'confocal_background'),
                 ('What is near-field coupling in SNOM?', 'snom'),
                 ('What does a microscope objective do?', 'microscope_basics')]
        for question, topic in cases:
            p = request(question=question)
            p.pop('guidance')
            result = chat.generate(p, mock=True)
            self.assertEqual(result['kind'], 'explain', question)
            self.assertEqual(result['knowledge_topics'], [topic])
            self.assert_english(result['answer'])

    def test_next_step_and_exit_remain_distinct_in_english(self):
        s = state('part_selected', ['learn:parts_exit', 'learn:na_start'])
        s.update(partExperiment='na_start', selectedPart='Upper Optical Assembly', partDescription='A teaching optical assembly.')
        for question, action in [('What should I do next?', 'learn:na_start'), ('How do I exit?', 'learn:parts_exit')]:
            result = chat.generate(request(s, question), mock=True)
            self.assertEqual(result['suggested_action_ids'], [action])
            self.assert_english(result['answer'])

    def test_english_specimen_request_uses_live_target(self):
        nav = snapshot()
        nav['targets'][0]['id'] = 'sample_red'
        p = request(question='Where can I find the red specimen?')
        p.pop('guidance')
        p['navigation'] = nav
        result = chat.generate(p, mock=True)
        self.assertEqual(result['suggested_action_ids'], ['highlight:sample_red'])
        self.assert_english(result['answer'])
        self.assert_english(json.dumps(navigation.context(nav)))

    def test_refusal_matches_client_and_system_prompt(self):
        root = Path(__file__).resolve().parents[2]
        client = (root/'Assets/m_Scripts/Assistant/AssistantChatClient.cs').read_text(encoding='utf-8-sig')
        self.assertIn('public const string Refusal = '+json.dumps(chat.REFUSAL)+';', client)
        self.assertIn(chat.REFUSAL, chat.KnowledgeBundle().rules)
        self.assert_english(chat.refusal()['answer'])

    def test_voice_transcription_keeps_original_language(self):
        body, _ = speech.multipart(b'example')
        self.assertNotIn(b'name="language"', body)
        self.assertNotIn(b'name="task"', body)
        self.assertTrue(speech.ENDPOINT.endswith('/audio/transcriptions'))


if __name__ == '__main__':
    unittest.main()
