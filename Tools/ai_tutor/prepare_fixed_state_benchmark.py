"""Validate and materialize the benchmark DESIGN. No network/model/Unity calls.

Run from any directory with Python 3.10+:
    python Tools/ai_tutor/prepare_fixed_state_benchmark.py
Outputs stay in docs/benchmarks/fixed_state_ai; never modifies production code.
"""
import copy
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path

import assistant_chat as chat
import guidance
import navigation

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "docs/benchmarks/fixed_state_ai"


def stable_id(label):
    return hashlib.sha256(label.encode("utf-8")).hexdigest()[:32]


def resolve(design, state_name, identity, patch=None):
    spec = design["states"][state_name]
    state = {**copy.deepcopy(design["defaults"]), **copy.deepcopy(spec["guidance"])}
    state.update(patch or {})
    state.update(snapshotId=stable_id(identity + ":guidance"), catalogVersion=design["catalog_version"])
    state["allowedActionIds"] = sorted(state["allowedActionIds"])
    nav = None
    if spec.get("navigation"):
        nav = copy.deepcopy(design["navigation_fixtures"][spec["navigation"]])
        nav["snapshotId"] = stable_id(identity + ":navigation")
    guidance.context(state)
    navigation.context(nav)
    return state, nav


def allowed(state, nav):
    return sorted(state["allowedActionIds"] + navigation.context(nav)["allowed_actions"])


def candidate(action_id):
    if action_id.startswith("learn:"):
        entry = guidance.ACTIONS[action_id]
        interaction, topic = entry["interaction_id"], entry["knowledge_topic"]
    else:
        interaction = action_id.removeprefix("highlight:")
        topic = navigation.TARGETS[interaction][2]
    return dict(kind="guide", answer="Design fixture only; no model result.",
                interaction_ids=[interaction], suggested_action_ids=[action_id], knowledge_topics=[topic])


def main():
    design = json.loads((FOLDER / "cases.json").read_text(encoding="utf-8"))
    assert design["status"] == "designed_not_executed"
    assert design["catalog_version"] == guidance.CATALOG["version"]
    cases = design["cases"]
    assert 24 <= len(cases) <= 30
    assert len({c["id"] for c in cases}) == len(cases)
    records, rows, editor_cases = [], [], []
    lines = ["# Fixed-state AI benchmark: case catalogue", "",
             "Generated from `cases.json`. Status: **design only; no model or Unity outcomes collected**.", "",
             "Read [PROTOCOL.md](PROTOCOL.md) before execution. `requests.jsonl` contains resolved request payloads; gold labels are kept separately in `cases.json`.", "",
             "A(s) below includes both `learn:` operations and `highlight:` navigation actions. G(s,q) is an acceptable set, not a unique answer string. Empty G means no action is required/helpful for that query.", "",
             "All states use desktop bindings. Unlisted scalar fields take `cases.json.defaults`; full fields, including descriptions, are expanded in each JSONL payload. The operation-only roaming states explicitly disable navigation. Navigation cases use a separate synthetic geometry fixture, pending Unity capture.", ""]
    for c in cases:
        state, nav = resolve(design, c["state"], c["id"])
        aset = allowed(state, nav)
        assert set(c["goal_actions"]) <= set(aset), c["id"]
        assert set(c["expected_kinds"]) <= {"guide", "explain", "clarify", "refuse"}
        if c["expected_kinds"] == ["guide"]:
            assert c["goal_actions"], c["id"]
        if c.get("expected_target"):
            assert c["goal_actions"] == ["highlight:" + c["expected_target"]]
        for action_id in c["goal_actions"]:
            chat.validate_answer(candidate(action_id), nav, state)
        spec = design["states"][c["state"]]
        compact_state = {k: v for k, v in state.items()
                         if k not in ("allowedActionIds", "snapshotId", "catalogVersion", "partDescription")
                         and (v != design["defaults"].get(k) or k == "mode")}
        lines.extend([f"## {c['id']} — {c['category']}", "",
                      f"- **State {c['state']}:** {spec['description']}",
                      f"- **State fields:** `{json.dumps(compact_state, ensure_ascii=False)}`",
                      f"- **Scene/setup prerequisites:** {spec['setup']}",
                      f"- **Query:** {c['query']}",
                      "- **A(s):** " + (", ".join(f"`{a}`" for a in aset) or "∅"),
                      "- **G(s,q):** " + (", ".join(f"`{a}`" for a in c["goal_actions"]) or "∅"),
                      "- **Expected kind(s):** " + ", ".join(f"`{k}`" for k in c["expected_kinds"]),
                      f"- **Expected target:** `{c.get('expected_target', 'none')}`",
                      f"- **Expected outcome:** {c['expected']}",
                      "- **Manual rubric (all applicable checks required):** " + " ".join(c["rubric"])])
        if c.get("history"):
            lines.append("- **Fixed history:** " + json.dumps(c["history"], ensure_ascii=False))
        lines.append("")
        for repeat in range(1, design["repetitions"] + 1):
            identity = f"{design['version']}:{c['id']}:{repeat}"
            state, nav = resolve(design, c["state"], identity)
            payload = dict(sessionId=stable_id(identity + ":session"), requestId=stable_id(identity + ":request"),
                           question=c["query"], history=c.get("history", []), guidance=state, navigation=nav)
            chat.validate_request(payload)
            if repeat == 1:
                editor_cases.append(dict(id=c["id"], category=c["category"], state=c["state"],
                                         description=spec["description"], setup=spec["setup"],
                                         expected=c["expected"], expected_kinds=c["expected_kinds"],
                                         goal_actions=c["goal_actions"], expected_target=c.get("expected_target", ""),
                                         rubric=c["rubric"], has_navigation=nav is not None, payload=payload))
            records.append(dict(case_id=c["id"], repetition=repeat, payload=payload))
            rows.append(dict(case_id=c["id"], repetition=repeat, status="not_run"))
    lines.extend(["## Deterministic delayed-response tests (not model accuracy)", "",
                  "Hold a valid response for s0; change the actual Unity state to s1; deliver the old response with its original IDs. Preserve s0 request IDs; do not silently replace them with s1 IDs. Record rejection, displayed text and reminder/marker existence. These are test specifications, not executed tests.", ""])
    for t in design["stale_tests"]:
        s0, n0 = resolve(design, t["s0"], t["id"] + ":s0")
        chat.validate_answer(candidate(t["response_action"]), n0, s0)
        resolve(design, t["s1"], t["id"] + ":s1", t.get("s1_patch"))
        lines.extend([f"### {t['id']} — {t['s0']} → {t['s1']}", "",
                      f"- Delayed response: `{t['response_action']}`.",
                      f"- Mutation: {t['mutation']}",
                      f"- Expected: {t['expected']}", ""])
    lines.extend(["## Acceptance controls", ""])
    for t in design["positive_controls"]:
        state, nav = resolve(design, t["state"], t["id"])
        chat.validate_answer(candidate(t["response_action"]), nav, state)
        lines.extend([f"- **{t['id']}**, `{t['state']}`, `{t['response_action']}`: {t['expected']}", ""])
    columns = ["case_id", "repetition", "status", "timestamp_utc", "model_requested", "model_returned",
               "prompt_version", "knowledge_version", "capture_provenance", "seed_or_not_supported",
               "raw_first_candidate_json", "raw_first_action_valid", "raw_first_goal_corresponding",
               "first_pass_json_schema_valid", "first_pass_contract_valid", "repair_attempted",
               "repair_candidate_json", "semantic_review_attempted", "semantic_verdict_json",
               "review_rejected", "canonical_pre_review_json", "gateway_final_json", "gateway_goal_corresponding",
               "client_identity_ok", "client_freshness_ok", "client_permissions_ok", "displayed_text",
               "displayed_action_id", "displayed_target_id", "displayed_action_valid",
               "final_goal_corresponding", "expected_kind_ok", "boundary_safe", "boundary_informative",
               "rubric_scope", "rubric_supported", "rubric_goal", "rubric_no_unsupported_claim",
               "case_rubric_pass", "rater_1", "rater_2", "adjudication", "final_task_success",
               "t0_request_sent_ms", "t1_first_candidate_ms", "t2_validation_complete_ms",
               "t3_semantic_review_complete_ms", "t4_gateway_returned_ms", "t5_client_displayed_ms",
               "provider_call_count", "error_code", "notes"]
    (FOLDER / "CASES.md").write_text("\n".join(lines), encoding="utf-8")
    (FOLDER / "requests.jsonl").write_text("\n".join(json.dumps(r, ensure_ascii=False) for r in records) + "\n", encoding="utf-8")
    (FOLDER / "editor_suite.json").write_text(json.dumps(dict(version=design["version"],
        catalog_version=design["catalog_version"], knowledge_version=chat.KnowledgeBundle().version,
        provenance="source_derived_fixture", cases=editor_cases), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    with (FOLDER / "RESULTS_TEMPLATE.csv").open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=columns)
        writer.writeheader()
        writer.writerows(rows)
    sources = ["Assets/Resources/AssistantGuidanceActions.json",
               "Assets/m_Scripts/Assistant/AssistantGuidance.cs", "Assets/m_Scripts/Assistant/AssistantNavigation.cs",
               "Assets/m_Scripts/Assistant/AssistantChatClient.cs", "Assets/m_Scripts/Assistant/AssistantChatPanel.cs",
               "Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs",
               "Assets/m_Scripts/Experiment/SpatialFrequencyExperimentController.cs",
               "Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs",
               "Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity",
               "Tools/ai_tutor/assistant_chat.py", "Tools/ai_tutor/guidance.py", "Tools/ai_tutor/navigation.py",
               "docs/assistant_knowledge/SYSTEM_PROMPT.md", "docs/assistant_knowledge/PROJECT_KNOWLEDGE.md",
               "docs/assistant_knowledge/interaction_catalog.json", "docs/benchmarks/fixed_state_ai/cases.json",
               "docs/benchmarks/fixed_state_ai/PROTOCOL.md", "Tools/ai_tutor/prepare_fixed_state_benchmark.py",
               "Assets/m_Scripts/Editor/AssistantBenchmarkWindow.cs", "Tools/ai_tutor/server.py",
               "Tools/ai_tutor/benchmark_trace.py"]
    manifest = dict(status="design_schema_validated_only", benchmark_version=design["version"],
                    case_count=len(cases), categories=dict(Counter(c["category"] for c in cases)),
                    request_count=len(records), stale_specs=len(design["stale_tests"]),
                    positive_control_specs=len(design["positive_controls"]),
                    prompt_version=chat.PROMPT_VERSION, knowledge_version=chat.KnowledgeBundle().version,
                    hashes={p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in sources})
    (FOLDER / "DESIGN_MANIFEST.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Design consistency checked: {len(cases)} cases; {len(records)} request fixtures; "
          f"{len(design['stale_tests'])} stale specifications; {len(design['positive_controls'])} acceptance controls.")
    print("No model calls, no Unity execution, no performance/accuracy results.")
    print("Generated CASES.md, requests.jsonl, editor_suite.json, RESULTS_TEMPLATE.csv and DESIGN_MANIFEST.json.")


if __name__ == "__main__":
    main()
