"""Loopback DeepSeek assistant gateway. Python 3.10+, standard library only.
Use --prompt-key for hidden key entry or --mock for network-free model simulation.
"""
import argparse
import collections
import getpass
import json
import os
from pathlib import Path
import re
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import assistant_chat
import benchmark_trace
import speech
from assistant_chat import model_config, require


class AssistantServer(ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self, address, mock=False, benchmark_traces=None):
        super().__init__(address, Handler)
        self.mock = mock
        self.benchmark_traces = Path(benchmark_traces).resolve() if benchmark_traces else None
        if self.benchmark_traces:
            self.benchmark_traces.mkdir(parents=True, exist_ok=True)
        self.slots = threading.BoundedSemaphore(2)
        self.rate_lock = threading.Lock()
        self.requests = collections.deque()
        self.assistant_knowledge = assistant_chat.KnowledgeBundle()

    def allow_request(self):
        with self.rate_lock:
            now = time.monotonic()
            while self.requests and self.requests[0] < now - 60:
                self.requests.popleft()
            if len(self.requests) >= 30:
                return False
            self.requests.append(now)
            return True


class Handler(BaseHTTPRequestHandler):
    def setup(self):
        super().setup()
        self.connection.settimeout(20)

    def log_message(self, *_):
        pass  # Do not log request bodies, secrets or learner answers.

    def send_json(self, status, payload):
        data = json.dumps(payload).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path != "/health":
            self.send_json(404, {"error": "not_found"}); return
        model, key = model_config()
        self.send_json(200, {"mode": "mock" if self.server.mock else "model", "configured": bool(key),
                             "provider": "deepseek", "model": model,
                             "assistantModel": assistant_chat.model_config()[0],
                             "benchmarkTracing": self.server.benchmark_traces is not None,
                             "speechConfigured": bool(os.environ.get("GROQ_API_KEY", "").strip()),
                             "speechModel": speech.MODEL,
                             "assistantKnowledgeVersion": self.server.assistant_knowledge.version})

    def do_POST(self):
        if self.path == "/speech":
            self.transcribe(); return
        if self.path != "/assistant":
            self.send_json(404, {"error": "not_found"}); return
        # Loopback prototype: reject browser-originated requests; no permissive CORS.
        if self.headers.get("Origin") or self.headers.get_content_type() != "application/json":
            self.send_json(403, {"error": "unsupported_client"}); return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            require(0 < length <= 65536)
            payload = assistant_chat.validate_request(json.loads(self.rfile.read(length)))
        except (ValueError, TypeError, KeyError, OverflowError):
            self.send_json(400, {"error": "invalid_request"}); return
        run_id = self.headers.get("X-Assistant-Benchmark", "")
        if run_id and not re.fullmatch(r"[a-f0-9]{32}", run_id):
            self.send_json(400, {"error": "invalid_benchmark_id"}); return
        if not self.server.allow_request():
            self.send_json(429, {"error": "rate_limit"}); return
        if not self.server.slots.acquire(blocking=False):
            self.send_json(429, {"error": "busy"}); return
        trace = (benchmark_trace.Trace(self.server.benchmark_traces, run_id, payload["requestId"])
                 if run_id and self.server.benchmark_traces else None)
        try:
            result = assistant_chat.generate(payload, self.server.mock, self.server.assistant_knowledge, trace=trace)
        except assistant_chat.ChatError as error:
            print("Assistant failure: " + error.code, file=sys.stderr)
            self.send_json(error.status, {"error": error.code})
        except Exception:
            print("Assistant failure: internal_error", file=sys.stderr)
            self.send_json(502, {"error": "guidance_unavailable"})
        else:
            self.send_json(200, result)
        finally:
            if trace is not None:
                try:
                    trace.save()
                except OSError:
                    print("Benchmark trace could not be saved; this run has incomplete traces.", file=sys.stderr)
            self.server.slots.release()

    def transcribe(self):
        if self.headers.get("Origin") or self.headers.get_content_type() != "audio/wav":
            self.send_json(403, {"error": "unsupported_client"}); return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            require(44 < length <= speech.MAX_BYTES)
        except (ValueError, TypeError):
            self.send_json(400, {"error": "invalid_audio"}); return
        if not self.server.allow_request() or not self.server.slots.acquire(blocking=False):
            self.send_json(429, {"error": "busy"}); return
        try:
            data = self.rfile.read(length)
            if len(data) != length:
                raise speech.SpeechError(400, "invalid_audio")
            result = speech.transcribe(data, self.server.mock)
            self.send_json(200, result)
        except speech.SpeechError as error:
            self.send_json(error.status, {"error": error.code})
        except (BrokenPipeError, ConnectionResetError):
            pass  # A cancelled client must not produce a request/audio log.
        except Exception:
            self.send_json(502, {"error": "speech_unavailable"})
        finally:
            self.server.slots.release()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--mock", action="store_true")
    parser.add_argument("--benchmark-traces", type=Path,
                        help="Save local traces only for requests carrying a valid benchmark run header")
    parser.add_argument("--prompt-groq-key", action="store_true", help="Read the optional speech key without echo")
    parser.add_argument("--prompt-key", action="store_true", help="Read a key without echo; keep it only in this process")
    args = parser.parse_args()
    if args.prompt_groq_key and not args.mock:
        if not sys.stdin.isatty():
            parser.error("Hidden key input requires an interactive terminal.")
        os.environ["GROQ_API_KEY"] = getpass.getpass("Groq API key (hidden, not saved): ").strip()
    if args.prompt_key and not args.mock:
        if not sys.stdin.isatty():
            parser.error("Hidden key input requires an interactive terminal.")
        os.environ["DEEPSEEK_API_KEY"] = getpass.getpass("DeepSeek API key (hidden, not saved): ").strip()
    if not args.mock and not model_config()[1]:
        parser.error("Set DEEPSEEK_API_KEY or use --prompt-key. Use --mock for integration testing.")
    print("Microscope gateway on 127.0.0.1:%d: /assistant + /speech (%s)" % (args.port, "mock" if args.mock else "model"))
    with AssistantServer(("127.0.0.1", args.port), args.mock, args.benchmark_traces) as server:
        server.serve_forever()
