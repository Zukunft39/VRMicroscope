import json
from pathlib import Path
import tempfile
import threading
import unittest
from unittest.mock import patch
import urllib.request
import urllib.error

import assistant_chat as chat
import benchmark_trace
import prepare_fixed_state_benchmark as design
import server
from test_guidance import request, answer


class BenchmarkTracing(unittest.TestCase):
    def test_trace_preserves_raw_candidate_before_canonical_replacement(self):
        events = []
        raw = answer("learn:na_adjust")
        raw["answer"] = "Raw model wording to preserve."
        verdict = {"in_scope": True, "supported": True, "contains_action_instructions": True}
        with patch.object(chat, "completion", side_effect=[raw, verdict]):
            result = chat.generate(request(), trace=lambda event, data: events.append((event, data)))
        candidate = next(data["candidate"] for event, data in events if event == "generation_candidate")
        canonical = next(data["answer"] for event, data in events if event == "candidate_validated")
        self.assertEqual(candidate["answer"], "Raw model wording to preserve.")
        self.assertNotEqual(candidate["answer"], canonical["answer"])
        self.assertEqual(result["answer"], canonical["answer"])
        self.assertEqual(sum(event == "provider_call_started" for event, _ in events), 2)
        self.assertIsNone(chat._trace.get())

    def test_repair_and_failed_review_are_separate_events(self):
        events = []
        verdict = {"in_scope": True, "supported": False, "contains_action_instructions": True}
        with patch.object(chat, "completion", side_effect=[answer("learn:sf_high"), answer(), verdict]):
            result = chat.generate(request(), trace=lambda event, data: events.append((event, data)))
        self.assertEqual(result["kind"], "refuse")
        self.assertEqual([data["attempt"] for event, data in events if event == "generation_started"], [0, 1])
        self.assertEqual(sum(event == "validation_failed" for event, _ in events), 1)
        self.assertFalse(next(data["accepted"] for event, data in events if event == "review_finished"))
        self.assertEqual(sum(event == "provider_call_started" for event, _ in events), 3)

    def test_timeout_is_recorded_without_details_and_scope_is_reset(self):
        events = []
        with patch.object(chat, "completion", side_effect=TimeoutError("private credential details")):
            with self.assertRaises(chat.ChatError):
                chat.generate(request(), trace=lambda event, data: events.append((event, data)))
        self.assertEqual(events[-1][1]["error_code"], "model_timeout")
        self.assertNotIn("private credential", json.dumps(events))
        self.assertIsNone(chat._trace.get())
        count = len(events)
        chat.generate(request(), mock=True)
        self.assertEqual(len(events), count)

    def test_trace_identifiers_cannot_escape_folder_and_duplicates_do_not_overwrite(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                benchmark_trace.Trace(folder, "../outside", "a" * 32)
            trace = benchmark_trace.Trace(folder, "a" * 32, "b" * 32)
            trace("test", {"value": 1})
            first, second = trace.save(), trace.save()
            self.assertNotEqual(first, second)
            self.assertTrue(first.is_relative_to(Path(folder)))
            self.assertEqual(json.loads(first.read_text())["events"][0]["value"], 1)

    def test_editor_payloads_do_not_include_gold_labels(self):
        spec = json.loads((design.FOLDER / "cases.json").read_text(encoding="utf-8"))
        for case in spec["cases"]:
            state, nav = design.resolve(spec, case["state"], case["id"])
            payload = dict(sessionId="a" * 32, requestId="b" * 32,
                           question=case["query"], history=case.get("history", []), guidance=state, navigation=nav)
            chat.validate_request(payload)
            self.assertTrue(set(case["goal_actions"]) <= set(design.allowed(state, nav)))
            self.assertNotIn("goal_actions", payload)
            self.assertNotIn("expected", payload)


class BenchmarkHttp(unittest.TestCase):
    def test_all_editor_cases_roundtrip_through_mock_gateway(self):
        suite = json.loads((design.FOLDER / "editor_suite.json").read_text(encoding="utf-8"))
        self.assertEqual(len(suite["cases"]), 26)
        gateway = server.AssistantServer(("127.0.0.1", 0), mock=True)
        thread = threading.Thread(target=gateway.serve_forever, daemon=True)
        thread.start()
        url = "http://127.0.0.1:%s/assistant" % gateway.server_address[1]
        try:
            for case in suite["cases"]:
                with self.subTest(case=case["id"]):
                    payload = dict(case["payload"])
                    if not case["has_navigation"]:
                        payload.pop("navigation", None)  # Matches the editor's OperationRequest serialization.
                    req = urllib.request.Request(url, json.dumps(payload).encode(), {"Content-Type": "application/json"})
                    with urllib.request.urlopen(req, timeout=5) as response:
                        result = json.load(response)
                    self.assertEqual(result["source"], "mock")
                    self.assertEqual(result["requestId"], payload["requestId"])
                    # Check compatibility only: keyword mock choices are not evaluated as model accuracy.
                    answer_fields = {k: result[k] for k in chat.FIELDS}
                    chat.validate_answer(answer_fields, payload.get("navigation"), payload["guidance"])
        finally:
            gateway.shutdown()
            gateway.server_close()
            thread.join(timeout=5)

    def test_trace_is_opt_in_per_request_and_wire_response_is_unchanged(self):
        with tempfile.TemporaryDirectory() as folder:
            gateway = server.AssistantServer(("127.0.0.1", 0), mock=True, benchmark_traces=folder)
            thread = threading.Thread(target=gateway.serve_forever, daemon=True)
            thread.start()
            url = "http://127.0.0.1:%s/assistant" % gateway.server_address[1]
            def send(run_id=None):
                headers = {"Content-Type": "application/json"}
                if run_id is not None:
                    headers["X-Assistant-Benchmark"] = run_id
                req = urllib.request.Request(url, json.dumps(request()).encode(), headers)
                with urllib.request.urlopen(req, timeout=5) as response:
                    return json.load(response)
            try:
                plain = send()
                self.assertEqual(list(Path(folder).rglob("*.json")), [])
                traced = send("a" * 32)
                self.assertEqual(plain, traced)
                with self.assertRaises(urllib.error.HTTPError) as error:
                    send("../escape")
                self.assertEqual(error.exception.code, 400)
                error.exception.close()
            finally:
                gateway.shutdown()
                gateway.server_close()
                thread.join(timeout=5)
            files = list(Path(folder).rglob("*.json"))
            self.assertEqual(len(files), 1)
            events = json.loads(files[0].read_text())["events"]
            self.assertEqual(events[-1]["event"], "gateway_result")
            self.assertEqual(events[0]["source"], "mock")


if __name__ == "__main__":
    unittest.main()
