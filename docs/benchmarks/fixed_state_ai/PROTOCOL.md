# Fixed-state reliability benchmark for VRMicroscope

Status: **design only — no AI benchmark results collected**. This protocol tests the current assistant implementation, not an imagined finer-grained action interface. The case catalogue is [CASES.md](CASES.md); editable labels and state definitions are in [cases.json](cases.json).

Automation is now available through **Tools > VR Microscope > AI Benchmark**. Follow [QUICK_START.md](QUICK_START.md) for single-case replay, a shuffled batch of 78 requests, optional recorded Unity snapshots and opt-in backend traces. Automatic checks do not replace the prose rubric or the separate live client tests.

## Assessment and intended claim

This is a useful, small addition to the paper: it separates permission conformance from learning-goal correspondence and tests the mechanisms underlying bounded guidance. It can establish reliability on a specified case suite. It cannot establish a general guarantee of semantic correctness, a learning benefit, or superiority over a state-unaware assistant without a separate comparison. Existing user-study evidence must be described independently.

The recommended design is **26 fixed model cases × 3 repetitions = 78 application requests**, plus **10 deterministic delayed-response tests** and **2 acceptance controls**. No participants are required to exercise the software, but the gold labels and prose judgments need qualified review.

| Primary stratum | Fixed cases | Repetitions | Application requests |
|---|---:|---:|---:|
| Action availability and workflow gates | 8 | 3 | 24 |
| Goal correspondence and component explanation | 8 | 3 | 24 |
| Unsupported requests, scope and blocked state | 6 | 3 | 18 |
| Spatial target selection | 4 | 3 | 12 |
| Total model cases | 26 | 3 | 78 |
| Delayed-response specifications | 10 | Deterministic client tests | No model required |
| Acceptance controls | 2 | Deterministic client tests | No model required |

The strata identify each case's primary purpose, not disjoint metrics. Most operational cases test both availability and correspondence. AV07 intentionally tests waiting despite a legal exit. GC08 intentionally tests explanation despite a legal exit. BD01 allows bounded explanation/clarification or a safe focus guide, subject to its explicit uncertainty rubric.

The 78 requests are **not necessarily 78 provider calls**. Current production generation permits one structural repair and one semantic repair for an otherwise supported non-guide answer that contains direct control instructions. Non-refusals are reviewed, and a semantic repair is reviewed again. A completed request can therefore invoke the model up to five times, although the normal path remains one generation plus one review. Refusals skip semantic review; failures can terminate earlier. Record actual call counts, tokens if available and failures, rather than estimating charges from the number of cases.

## Alignment with the implementation

The inspected sources are fingerprinted in [DESIGN_MANIFEST.json](DESIGN_MANIFEST.json).

| Implementation | Consequence for this benchmark |
|---|---|
| `AssistantGuidance.Read()` derives IDs from mode, usable controls, proximity and desktop bindings. | A(s) must be captured with the actual UI/control configuration; backend JSON validation alone cannot establish reachability of a state. |
| Action catalogue exposes `learn:focus` only. | Do not label imaginary `coarse_focus_up` or `fine_focus_down` IDs as supported. Direction-specific claims require prose review. |
| `learn:parts_select` is one general component-selection action. | Its ID cannot prove that the learner was told to select one unique component; inspect its canonical text as well. |
| NA entry is on Upper Optical Assembly; spatial-frequency entry is on Objective Lens. | GC03/GC04 test actual links, not conventional assumptions about instrument anatomy. |
| `installedProbe` is set when installation begins, before it finishes. | AV07 must be conditioned on `snomPhase=InstallingProbe`, not merely an installed-probe index. |
| Navigation IDs include `snom_entry`, `na_experiment`, `spatial_frequency`, `sample_blue`. | Use these exact IDs. There is no `snom_station` or `blue_specimen` target. |
| Backend `navigation.context()` computes snapshot direction/distance; client `Navigation.Apply()` recomputes live geometry. | Neither computation is model reasoning accuracy. Target selection is the model outcome. |
| `validate_answer()` replaces operation prose with `guidance.render()` before semantic review. | Preserve the raw candidate separately; the reviewer sees canonical operation prose, not necessarily the model's wording. |
| Semantic review returns `in_scope`, `supported`, `contains_action_instructions`. | Goal correspondence is an instruction within review, not a separately emitted score. Review rejection alone does not reveal the failure reason or prove correct goal selection. |
| A supported non-guide candidate rejected only for direct control instructions receives one bounded semantic repair and a second review. | Count this separately from structural/JSON repair. The repair must either remove direct steps from the explanation or select exactly one currently allowed guide action; a second rejection fails closed. |
| `Guidance.Apply()` checks snapshot identity, serialized current-state signature and current action mappings. | Even an unchanged action set can produce rejection if NA or tour stage changed. ST04/ST07 measure this behavior separately. |
| Navigation checks live eligibility, identity, active target/bounds and arrival, rather than exact player-pose equality. | A head turn is not automatically stale navigation. PC02 should be accepted with updated geometry. |

The synthetic fixtures are not claims that runtime snapshots have already been recorded. The operation-only roaming fixtures intentionally set `navigationEnabled=false` to isolate action guidance. Non-roaming modes naturally offer no navigation. The navigation stratum sets it true and uses a synthetic, fixed arrangement of the actual eight target IDs. This condition must be disclosed; it does not cover competition between navigation and operations at the same nearby interface. Add that stratum later if needed, without silently broadening the current claim.

Static transform origins in `navigation_scene_audit.json` are not current renderer-bounds centers or player positions, so they were not misrepresented as live inputs. Before publication, either replace the fixtures with recorded Unity captures or explicitly report the experiment as source-derived synthetic-state replay and restrict conclusions accordingly.

## Formal labels and response types

Let A_op(s) be the exact `guidance.allowedActionIds`. Let A_nav(s) be the `highlight:` IDs derived from offered navigation targets. Define:

$$
A(s)=A_{op}(s)\cup A_{nav}(s),\qquad G(s,q,h)\subseteq A(s).
$$

History h is fixed and empty except GC01; this prevents an underspecified “next step” from depending on unseen chat history. G is a set of acceptable action IDs. AV05 permits three probe choices; GC06 permits two higher-frequency settings. For a conceptual, refusal or waiting response, G can be empty even when A contains a legal exit. Do not score `empty in empty` as success: evaluate the expected response kind and prose rubric.

The actual JSON kinds are `guide`, `explain`, `clarify`, `refuse`. Location is a guide with `highlight:target`, not a fifth kind. A valid guide also requires its exact `interaction_ids` and `knowledge_topics` mapping; membership of one ID alone does not establish response-contract validity.

Gold labels are evaluation data only. Send only the `payload` field of a JSONL record to the gateway. Never include G, expected outcomes, category labels or scoring rubric in the model messages. The generator's canonical action descriptions remain in the normal production context; that is part of the deployed system under test.

Before running the model, ask two people familiar with the implemented optics lessons and controls to independently check the proposed G sets, prerequisite logic and prose rubric. Resolve disagreements and freeze the design version. If only one author checks them, disclose author-defined labels rather than claiming independent expert validation. Do not alter gold labels after seeing outputs without reporting a new version and rerunning the affected suite.

## Capture and execution procedure

1. Freeze source commit or file hashes, knowledge bundle, system prompt, action catalogue, device bindings and settings. Record the actual requested and returned model identifier; an alias is not proof of a permanently fixed model revision. The current code uses `temperature=0.2`, `max_tokens=1500`, disabled thinking, non-streaming JSON output, and an 18-second timeout per upstream call. Do not change these for this benchmark without declaring a new condition. Record an unavailable random seed as unsupported, not as a fixed seed.
2. Reproduce each state in Unity using the prerequisites listed in CASES.md. Capture `Guidance.Capture()` and `Navigation.Capture()` together. Verify the complete offered set and control usability, not only the desired action. Record descriptions, mode, phase and UI evidence. Replace source-derived snapshots with these frozen captures and review any changed labels before collection. This includes checking `observationPoint` and SNOM's retained stage value.
3. Do not freeze Unity with `Time.timeScale=0`: it changes guidance to blocked. For offline model calls, freeze the captured JSON. Pause the SNOM tour with its own Pause control before recording S_TOUR. A genuinely changing state during a later Unity replay belongs to the stale-test condition, not a silent fixed-state failure.
4. Run three randomized, recorded-order passes over the 26 cases, in separate sessions. Only GC01 gets the listed fixed history. Hold all semantic fields constant within repetitions; distinct session/request/snapshot IDs are bookkeeping. Do not share unrelated history across cases or substitute mock responses. Avoid concurrent requests in the same Unity assistant instance because a newer capture changes pending identity.
5. Save the raw first candidate before validation, any repair candidate, canonical pre-review answer, review verdict, gateway answer and error separately. The gateway wire response still contains only the final answer and metadata. Start the gateway with `--benchmark-traces BenchmarkRuns/traces` to record the full local trace for tagged requests from the benchmark window. Untagged normal chats are not traced. The preparation script supplies data; the editor window executes the sequential run. Runs without traces cannot establish raw/repair/review metrics.
6. For displayed metrics, replay the matching response into an unchanged Unity state through the actual chat client and `Apply()` path. Record IDs, final displayed text, reminder/marker creation and any rejection. A backend-only run may report gateway-level metrics but must leave displayed metrics blank. Do not equate a response's retained `kind=guide` with actual guidance presentation: Apply may replace its text with a state-change notice. Operation reminders are intentionally hidden while chat blocks gameplay or an assistant message is visible; close those overlays without changing experiment state before checking reminder visibility. Time the chat answer presentation separately from this manual dismissal.
7. Run ST01–ST10 by delaying a structurally valid response, mutating the real scene, then delivering the original request/response IDs. These tests do not need a paid model call. Run PC01/PC02 as acceptance controls so an implementation that rejects everything cannot pass. Save evidence and actual outcomes; specifications in this folder are not passing test results.
8. Review outputs against the frozen rubric, preserve both raters' original labels, adjudicate differences, then aggregate. Keep all timeouts, refusals and schema/review errors; never replace failures with a successful retry and retain only the replacement.

If resources allow a later comparison, use the same questions and labels with a clearly specified state-ablated assistant. Without such a baseline, the current benchmark supports bounded-system reliability, not a causal claim that state awareness increased accuracy by a particular amount.

## Metrics and denominators

The primary required-operation set is AV01–AV06, AV08, GC01–GC07: **14 scenarios / 42 trials**. Navigation is NV01–NV04: **4 / 12**. BD01 is deliberately excluded from mandatory-operation denominators because a bounded non-action answer may be correct. AV07 and GC08 are likewise non-action goals. Report category breakdowns alongside any pooled number.

| Metric | Numerator / denominator and interpretation |
|---|---|
| Raw candidate action availability | Trials in the required-operation set whose **first** candidate proposes exactly one action in A_op(s), divided by all 42 scheduled required-operation trials. Malformed/no candidate/refusal/timeouts are failures for this unconditional metric; also report conditional validity among returned action candidates. |
| Raw goal correspondence | First candidate action in G for the same 42 trials / 42. This tests selected action, separately from whether its prose is supported. |
| Gateway goal correspondence | Final gateway action in G / 42. Report blocked/refused/error counts so a conservative reviewer cannot inflate the rate by withholding hard cases. |
| Final displayed availability | Actual displayed operational guides with action in the **live** A_op(s) / all actual displayed operational guides. Report the numerator, denominator and display coverage. Zero displayed guides means N/A, not 100%. This is observed client conformance, not model accuracy or a universal proof. |
| Final displayed goal correspondence | Trials producing a displayed operational guide in G / all 42 required-operation trials; also report the conditional fraction among displayed guides. A withheld or timed-out required answer is not successful guidance. |
| Navigation target selection | Correct `highlight:` action, interaction ID and target / all 12 navigation trials, reported both raw and final. Identical geometry does not make `parts` and `na_experiment` semantically interchangeable. Live direction/distance and marker success are separate deterministic/client measures. |
| Unsupported/scope handling | BD01–BD06 satisfying expected kind and full case rubric / 18. Also publish each subtype: missing information, absent feature, unrelated request, blocked state. This suite is not six instances of the same refusal task. |
| Boundary safety vs usefulness | Separately record no unsupported capability/claim, and whether the response explains the relevant limit or asks a useful clarification. A fixed refusal can be safe but uninformative; for AV07, BD01 and BD06, a generic refusal does not meet the full task rubric. |
| Non-action goal success | AV07 and GC08 expected kind + full rubric / 6. Prevents a legal exit from counting as a correct next-step or component explanation. |
| First-pass schema success | Well-formed first JSON candidate with the five-field/type contract / all 78 scheduled trials; report provider/transport failures separately. Full current-context contract validity additionally requires valid actions, topic mappings and permissible text. Keep JSON/schema and full validation separate. |
| Repair frequency | Report structural repair and semantic repair separately, each over 78 trials, with success conditional on that repair being attempted. Structural repair covers schema, identifier and current-action validation errors. Semantic repair covers a supported non-guide answer that included direct control instructions. |
| Semantic-review rejection | Non-refusal candidates receiving a valid rejecting review / candidates receiving a valid semantic review. Report skipped reviews and review-format/network errors separately; these are not negative review verdicts. |
| Final task success | Expected kind + required goal/target + all applicable manual checks + correct final client behavior / all 78 scheduled trials. Report gateway-only success separately if client replay is not performed. |
| Stale rejection | ST tests correctly preventing stale actionable presentation / 10. Also report ST04/ST07 as same-permission signature rejections, and failures of PC01/PC02 as false rejections. No language-model accuracy claim. |

For explanations and clarifications, record yes/no for scope, support by supplied material, relevance to the stated goal and absence of unsupported factual claims, plus each case-specific criterion. Do not use BLEU/ROUGE against one preferred sentence. For operation guides, score the raw prose separately from canonical displayed prose; model hallucinations discarded by the client must not be credited as correct raw reasoning.

BD01 is a useful limitation probe: the current canonical focus message offers safe controls but does not explicitly acknowledge missing diagnostic evidence. A safe action alone can therefore pass availability while failing the uncertainty/helpfulness rubric. Record this outcome rather than weakening the criterion to guarantee a pass.

## Timing and traces

Use monotonic elapsed timestamps within each process. If Unity and Python are on different machines, do not subtract their absolute clocks; record client end-to-end and server phase durations separately. CSV timestamp columns are offsets from the relevant trace origin, with clock origin documented in run metadata.

| Timestamp | Meaning |
|---|---|
| t0 | Application request sent |
| t1 | First upstream candidate available; record a transport/parse failure separately |
| t2 | Validation finished for the accepted candidate or terminal validation failure; record each attempt separately in a detailed trace |
| t3 | Semantic review finished, or N/A when skipped |
| t4 | Gateway response/error returned |
| t5 | Unity final presentation/rejection completed |

Report median and P95 gateway latency (t4−t0 measured by the gateway caller) and client end-to-end latency (t5−t0 measured in Unity), with sample counts. State whether percentiles are nearest-rank or interpolated; choose before examining results. Report successful-response latency separately from timeout/error latency and include failure counts. Never turn a timeout into a fast successful response or omit it silently.

The three repeats are not 78 independent scenarios. Report counts per scenario (0/3–3/3), category rates and denominators. With only three repetitions per case, keep uncertainty claims modest; if reporting pooled confidence intervals, resample by scenario rather than pretending repeated trials are independent new tasks. The suite is small and author-designed, so generalization beyond the sampled functions is limited.

## Files and preparation

| File | Purpose |
|---|---|
| `cases.json` | Proposed state/setup definitions, questions, acceptable G sets, expected kinds/targets, manual rubric and stale specifications. |
| `CASES.md` | Human-readable case catalogue, with State, Query, A(s), G(s,q), expected outcome for every case. |
| `requests.jsonl` | 78 full request fixtures. Submit only a record's `payload`; no gold labels are included inside it. Synthetic/source-derived until runtime captures replace them. |
| `editor_suite.json` | Generated case data loaded by the Unity Editor benchmark window; labels remain local to scoring. |
| `RESULTS_TEMPLATE.csv` | 78 empty result rows marked `not_run`. Copy to a new run directory before use; do not write measured results into this generated template. |
| `DESIGN_MANIFEST.json` | Source hashes and design consistency metadata; not an experiment report. |

Rebuild/check the design from the repository root:

```powershell
python Tools/ai_tutor/prepare_fixed_state_benchmark.py
```

This command checks snapshot schemas, real IDs, G ⊆ A, response mappings and request contracts using existing backend validators; generates the catalogue, request fixtures and blank template; and makes **no network calls**. It does not establish Unity reachability, semantic correctness, stale rejection or AI accuracy. Generated files are overwritten, so save collected runs separately. Preserve a frozen copy of the design and manifest before collecting results.

Keep the paper's “evaluation not yet conducted” statement until real model runs, manual review and the claimed client tests have actually been completed. Do not insert example percentages into the Results section.
