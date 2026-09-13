import io
import json
import struct
import threading
import unittest
import urllib.error
import urllib.request
import wave
from unittest.mock import patch

import server
import speech


def wav(seconds=1, rate=16000, channels=1, value=1000):
    output = io.BytesIO()
    with wave.open(output, "wb") as audio:
        audio.setnchannels(channels)
        audio.setsampwidth(2)
        audio.setframerate(rate)
        audio.writeframes(struct.pack("<h", value) * int(seconds * rate * channels))
    return output.getvalue()


class SpeechContracts(unittest.TestCase):
    def test_valid_recording_boundaries(self):
        for seconds, rate in ((.5, 8000), (30, 48000), (1, 16000)):
            speech.validate_audio(wav(seconds, rate))

    def test_reject_invalid_or_truncated_audio_before_provider(self):
        for data in (b"bad", wav(.1), wav(31), wav(channels=2), wav(rate=96000), wav()[:-2]):
            with self.subTest(size=len(data)), patch.object(speech.urllib.request, "urlopen") as call:
                with self.assertRaises(speech.SpeechError) as error:
                    speech.transcribe(data)
                self.assertEqual(error.exception.code, "invalid_audio")
                call.assert_not_called()

    def test_silence_does_not_call_provider(self):
        with patch.object(speech.urllib.request, "urlopen") as call:
            with self.assertRaises(speech.SpeechError) as error:
                speech.transcribe(wav(value=0))
            self.assertEqual(error.exception.code, "no_speech")
            call.assert_not_called()

    def test_missing_key(self):
        with patch.dict(speech.os.environ, {"GROQ_API_KEY": ""}):
            with self.assertRaises(speech.SpeechError) as error: speech.transcribe(wav())
        self.assertEqual(error.exception.code, "speech_not_configured")

    def test_mock_is_explicit_and_offline(self):
        with patch.object(speech.urllib.request, "urlopen", side_effect=AssertionError("Network")):
            self.assertEqual(speech.transcribe(wav(), True)["source"], "mock")

    def test_chinese_and_english_preserved_and_correct_multipart(self):
        data = wav()
        for text in ("数值孔径如何影响分辨率？", "How does numerical aperture affect resolution?", "SNOM 的 probe 有什么作用？"):
            with patch.dict(speech.os.environ, {"GROQ_API_KEY": "fake-unit-test-key"}), \
                    patch.object(speech.urllib.request, "urlopen", return_value=io.BytesIO(json.dumps({"text": text}).encode())) as call:
                result = speech.transcribe(data)
            self.assertEqual(result["text"], text)
            request = call.call_args.args[0]
            self.assertEqual(request.full_url, speech.ENDPOINT)
            self.assertTrue(request.full_url.endswith("/transcriptions"))
            self.assertIn(b'filename="speech.wav"', request.data)
            self.assertIn(data, request.data)
            self.assertIn(speech.MODEL.encode(), request.data)
            self.assertNotIn(b'name="language"', request.data)
            self.assertNotIn(b'name="prompt"', request.data)

    def test_provider_errors_are_sanitized(self):
        for status, code in ((401, "speech_key_rejected"), (403, "speech_key_rejected"),
                             (429, "speech_rate_limit"), (500, "speech_unavailable")):
            failure = urllib.error.HTTPError(speech.ENDPOINT, status, "private provider body", {}, io.BytesIO(b"secret"))
            with patch.dict(speech.os.environ, {"GROQ_API_KEY": "fake-unit-test-key"}), \
                    patch.object(speech.urllib.request, "urlopen", side_effect=failure):
                with self.assertRaises(speech.SpeechError) as error: speech.transcribe(wav())
            self.assertEqual(str(error.exception), code)

    def test_timeout(self):
        with patch.dict(speech.os.environ, {"GROQ_API_KEY": "fake-unit-test-key"}), \
                patch.object(speech.urllib.request, "urlopen", side_effect=TimeoutError()):
            with self.assertRaises(speech.SpeechError) as error: speech.transcribe(wav())
        self.assertEqual(error.exception.status, 504)

    def test_invalid_provider_results(self):
        cases = ((b"broken", "invalid_transcription"), (b"{}", "invalid_transcription"),
                 (b'{"text": " "}', "no_speech"),
                 (json.dumps({"text": "x" * 1001}).encode(), "transcript_too_long"),
                 (b"x" * 65537, "invalid_transcription"))
        for raw, code in cases:
            with patch.dict(speech.os.environ, {"GROQ_API_KEY": "fake-unit-test-key"}), \
                    patch.object(speech.urllib.request, "urlopen", return_value=io.BytesIO(raw)):
                with self.assertRaises(speech.SpeechError) as error: speech.transcribe(wav())
            self.assertEqual(error.exception.code, code)


class SpeechHttp(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.gateway = server.AssistantServer(("127.0.0.1", 0), mock=True)
        cls.thread = threading.Thread(target=cls.gateway.serve_forever, daemon=True)
        cls.thread.start()
        cls.url = "http://127.0.0.1:%d" % cls.gateway.server_port

    @classmethod
    def tearDownClass(cls):
        cls.gateway.shutdown(); cls.gateway.server_close(); cls.thread.join()

    def post(self, data, headers=None):
        request = urllib.request.Request(self.url + "/speech", data=data,
                                         headers=headers or {"Content-Type": "audio/wav"})
        try: response = urllib.request.urlopen(request, timeout=3)
        except urllib.error.HTTPError as error: response = error
        with response:
            return response.status, json.load(response)

    def test_wav_round_trip(self):
        status, result = self.post(wav())
        self.assertEqual(status, 200)
        self.assertEqual(result["source"], "mock")

    def test_reject_browser_and_wrong_content_type(self):
        for headers in ({"Content-Type": "application/json"}, {"Content-Type": "audio/wav", "Origin": "https://example.com"}):
            self.assertEqual(self.post(wav(), headers)[0], 403)

    def test_silence_error_round_trip(self):
        self.assertEqual(self.post(wav(value=0)), (422, {"error": "no_speech"}))

    def test_invalid_declared_lengths_rejected_before_read(self):
        for length in ("invalid", "0", "-1", str(speech.MAX_BYTES + 1)):
            status, result = self.post(b"short", {"Content-Type": "audio/wav", "Content-Length": length})
            self.assertEqual((status, result), (400, {"error": "invalid_audio"}))

    def test_health_reports_speech_without_exposing_key(self):
        with patch.dict(speech.os.environ, {"GROQ_API_KEY": "fake-unit-test-key"}), \
                urllib.request.urlopen(self.url + "/health", timeout=3) as response:
            result = json.load(response)
        self.assertTrue(result["speechConfigured"])
        self.assertNotIn("fake-unit-test-key", json.dumps(result))

    def test_provider_failure_does_not_block_future_requests(self):
        with patch.object(speech, "transcribe", side_effect=speech.SpeechError(504, "speech_timeout")):
            self.assertEqual(self.post(wav())[0], 504)
        self.assertEqual(self.post(wav())[0], 200)

    def test_busy_does_not_start_transcription(self):
        self.gateway.slots.acquire(); self.gateway.slots.acquire()
        try:
            with patch.object(speech, "transcribe") as call:
                self.assertEqual(self.post(wav())[0], 429)
                call.assert_not_called()
        finally:
            self.gateway.slots.release(); self.gateway.slots.release()


if __name__ == "__main__":
    unittest.main()
