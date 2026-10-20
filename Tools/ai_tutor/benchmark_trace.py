"""Explicitly enabled local traces for labelled benchmark requests, not normal chats."""
from datetime import datetime, timezone
import json
from pathlib import Path
import re
import time
import uuid


class Trace:
    def __init__(self, folder, run_id, request_id):
        if not re.fullmatch(r"[a-f0-9]{32}", run_id or ""):
            raise ValueError("Invalid benchmark run ID")
        if not re.fullmatch(r"[a-f0-9]{32}", request_id or ""):
            raise ValueError("Invalid benchmark request ID")
        self.folder = Path(folder) / run_id
        self.request_id = request_id
        self.run_id = run_id
        self.started = time.perf_counter()
        self.timestamp = datetime.now(timezone.utc).isoformat()
        self.events = []

    def __call__(self, event, data):
        self.events.append(dict(event=event, elapsed_ms=round((time.perf_counter() - self.started) * 1000, 3), **data))

    def save(self):
        self.folder.mkdir(parents=True, exist_ok=True)
        # Unique suffix prevents accidental overwriting if a caller reuses request IDs.
        path = self.folder / (self.request_id + "_" + uuid.uuid4().hex[:8] + ".json")
        with path.open("x", encoding="utf-8") as out:
            json.dump(dict(run_id=self.run_id, request_id=self.request_id, timestamp_utc=self.timestamp,
                           clock="server_perf_counter_elapsed_ms", events=self.events), out, ensure_ascii=False, indent=2)
        return path
