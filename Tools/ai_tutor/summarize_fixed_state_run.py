"""Read-only aggregation of saved gateway replay and provider traces (no API calls)."""
import argparse
import csv
import json
import math
import statistics
from collections import Counter
from pathlib import Path


def summarize(folder):
    plan = json.loads((folder / "plan.json").read_text(encoding="utf-8-sig"))
    trials = {t["payload"]["requestId"]: t for t in plan["trials"]}
    assert len(trials) == len(plan["trials"]), "Duplicate planned request IDs"
    with (folder / "results.csv").open(encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    assert len(rows) == len(trials)
    assert {r["request_id"] for r in rows} == set(trials)
    trace_dir = folder.parent / "traces" / plan["run_id"]
    traces = [json.loads(p.read_text(encoding="utf-8-sig")) for p in trace_dir.glob("*.json")]
    assert len(traces) == len(trials)
    assert {t["request_id"] for t in traces} == set(trials)
    metrics = Counter()
    events = Counter()
    versions = Counter()
    models = Counter()
    # Required-operation cases exclude waiting, explanation, optional guidance and navigation.
    required = {"AV01", "AV02", "AV03", "AV04", "AV05", "AV06", "AV08"}
    required.update(f"GC{i:02d}" for i in range(1, 8))
    for trace in traces:
        request_id = trace["request_id"]
        trial = trials[request_id]
        result = json.loads((folder / (request_id + ".result.json")).read_text(encoding="utf-8-sig"))
        final = result.get("response") or {}
        for event in trace["events"]:
            events[event["event"]] += 1
            if event["event"] == "provider_content":
                models[event.get("model_returned", "unknown")] += 1
            if event["event"] == "review_finished":
                metrics["reviews_accepted" if event["accepted"] else "reviews_rejected"] += 1
        candidates = [e for e in trace["events"] if e["event"] == "generation_candidate"]
        raw = candidates[0]["candidate"] if candidates else {}
        validated = [e for e in trace["events"] if e["event"] == "candidate_validated" and e["attempt"] == 0]
        metrics["first_pass_validated"] += bool(validated)
        versions[final.get("promptVersion", "missing") + "/" + final.get("knowledgeVersion", "missing")] += 1
        returned = [e["response"] for e in trace["events"] if e["event"] == "gateway_result"]
        assert len(returned) == 1
        for key in ("kind", "answer", "interaction_ids", "suggested_action_ids", "knowledge_topics", "promptVersion", "knowledgeVersion"):
            assert returned[0][key] == final[key], (request_id, key)
        for label, candidate in (("raw", raw), ("final", final)):
            ids = candidate.get("suggested_action_ids", [])
            guide = candidate.get("kind") == "guide" and len(ids) == 1
            if trial["case_id"] in required:
                metrics[label + "_operation_n"] += 1
                allowed = trial["payload"]["guidance"]["allowedActionIds"]
                metrics[label + "_operation_available"] += guide and ids[0] in allowed
                metrics[label + "_operation_goal_match"] += guide and ids[0] in trial["goal_actions"]
            if trial["category"] == "navigation":
                metrics[label + "_navigation_n"] += 1
                metrics[label + "_navigation_match"] += (guide and ids[0] in trial["goal_actions"]
                    and candidate.get("interaction_ids") == [trial["expected_target"]])
    times = sorted(float(r["latency_ms"]) for r in rows)
    return {
        "run_id": plan["run_id"], "trials": len(rows), "cases": len({r["case_id"] for r in rows}),
        "versions": dict(versions), "returned_provider_models": dict(models),
        "metrics": dict(metrics), "events": dict(events),
        "csv_counts": {field: dict(Counter(r[field] for r in rows)) for field in (
            "status", "kind", "contract_valid", "expected_kind_ok", "action_available", "goal_corresponding",
            "target_correct", "provenance", "client_apply_executed", "manual_review_pending", "error_code")},
        "latency_ms": {"median": statistics.median(times), "p95_nearest_rank": times[math.ceil(.95 * len(times)) - 1],
                       "min": min(times), "max": max(times)},
        "scope": "Gateway replay only; automatic labels do not constitute independent human semantic ratings."
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", type=Path)
    print(json.dumps(summarize(parser.parse_args().folder), indent=2))
