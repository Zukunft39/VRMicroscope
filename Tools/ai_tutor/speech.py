"""Bounded, in-memory WAV transcription. Never translate or persist recordings."""
import io
import json
import os
import struct
import urllib.error
import urllib.request
import uuid
import wave

ENDPOINT = "https://api.groq.com/openai/v1/audio/transcriptions"
MODEL = "whisper-large-v3-turbo"
MAX_BYTES = 44 + 30 * 48000 * 2


class SpeechError(Exception):
    def __init__(self, status, code):
        super().__init__(code)
        self.status, self.code = status, code


def validate_audio(data):
    try:
        if not 44 < len(data) <= MAX_BYTES:
            raise ValueError()
        with wave.open(io.BytesIO(data), "rb") as audio:
            frames, rate = audio.getnframes(), audio.getframerate()
            if (audio.getnchannels() != 1 or audio.getsampwidth() != 2 or
                    audio.getcomptype() != "NONE" or not 8000 <= rate <= 48000 or
                    not rate // 2 <= frames <= 30 * rate):
                raise ValueError()
            pcm = audio.readframes(frames)
            if len(pcm) != frames * 2:
                raise ValueError()
    except (ValueError, EOFError, wave.Error, struct.error):
        raise SpeechError(400, "invalid_audio") from None
    # Reject digital silence before a billable request. This is not a speech detector.
    if not any(abs(value[0]) > 32 for value in struct.iter_unpack("<h", pcm)):
        raise SpeechError(422, "no_speech")


def multipart(data):
    boundary = "microscope_" + uuid.uuid4().hex
    parts = []
    # Omit language and prompt so that the model detects the spoken language.
    for name, value in {"model": MODEL, "response_format": "json", "temperature": "0"}.items():
        parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n'
                      f'{value}\r\n').encode())
    parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="file"; '
                  'filename="speech.wav"\r\nContent-Type: audio/wav\r\n\r\n').encode())
    parts.extend([data, f"\r\n--{boundary}--\r\n".encode()])
    return b"".join(parts), "multipart/form-data; boundary=" + boundary


def transcribe(data, mock=False):
    validate_audio(data)
    if mock:
        return {"text": "数值孔径与分辨率有什么关系？", "source": "mock", "model": MODEL}
    key = os.environ.get("GROQ_API_KEY", "").strip()
    if not key:
        raise SpeechError(503, "speech_not_configured")
    body, content_type = multipart(data)
    request = urllib.request.Request(ENDPOINT, data=body, headers={
        "Authorization": "Bearer " + key, "Content-Type": content_type,
        "User-Agent": "VRMicroscope-Speech/1.0"})
    try:
        with urllib.request.urlopen(request, timeout=35) as response:
            raw = response.read(65537)
        if len(raw) > 65536:
            raise ValueError()
        result = json.loads(raw)
        text = result.get("text") if isinstance(result, dict) else None
        if not isinstance(text, str):
            raise ValueError()
        text = text.strip()
        if not text:
            raise SpeechError(422, "no_speech")
        if len(text) > 1000:
            raise SpeechError(422, "transcript_too_long")
        return {"text": text, "source": "model", "model": MODEL}
    except urllib.error.HTTPError as error:
        code = error.code
        error.close()
        if code == 429:
            raise SpeechError(429, "speech_rate_limit") from None
        if code in (401, 403):
            raise SpeechError(503, "speech_key_rejected") from None
        raise SpeechError(502, "speech_unavailable") from None
    except (TimeoutError, urllib.error.URLError):
        raise SpeechError(504, "speech_timeout") from None
    except (ValueError, TypeError, UnicodeError):
        raise SpeechError(502, "invalid_transcription") from None
