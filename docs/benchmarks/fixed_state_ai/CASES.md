# Fixed-state AI benchmark: case catalogue

Generated from `cases.json`. Status: **design only; no model or Unity outcomes collected**.

Read [PROTOCOL.md](PROTOCOL.md) before execution. `requests.jsonl` contains resolved request payloads; gold labels are kept separately in `cases.json`.

A(s) below includes both `learn:` operations and `highlight:` navigation actions. G(s,q) is an acceptable set, not a unique answer string. Empty G means no action is required/helpful for that query.

All states use desktop bindings. Unlisted scalar fields take `cases.json.defaults`; full fields, including descriptions, are expanded in each JSONL payload. The operation-only roaming states explicitly disable navigation. Navigation cases use a separate synthetic geometry fixture, pending Unity capture.

## AV01 — availability

- **State S_PICK:** Roaming beside a selectable specimen, without a placed specimen.
- **State fields:** `{"mode": "Roaming"}`
- **Scene/setup prerequisites:** Microscope interaction and NearMicroscope are false; AssistantCanPick is true; SNOM is out of range. Navigation is explicitly disabled for the operation-only stratum. Default desktop bindings are active.
- **Query:** I want to observe a specimen, but I have not picked one up. What is my next action?
- **A(s):** `learn:sample_pick`
- **G(s,q):** `learn:sample_pick`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide pickup of the currently selectable specimen. Do not skip to placement or observation.
- **Manual rubric (all applicable checks required):** Distinguishes pickup from placement. Only the offered pickup operation is recommended.

## AV02 — availability

- **State S_HELD:** Roaming at the microscope with a held specimen and an empty stage.
- **State fields:** `{"mode": "Roaming", "hasSample": true}`
- **Scene/setup prerequisites:** NearMicroscope and AssistantCanInteract are true; objective is stationary. No loose specimen is currently selectable (AssistantCanPick=false). Navigation disabled; default desktop bindings.
- **Query:** I am holding the specimen at the microscope. How do I put it on the empty stage?
- **A(s):** `learn:assembly_enter`, `learn:brightness`, `learn:light`, `learn:objective`, `learn:sample_place`
- **G(s,q):** `learn:sample_place`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide Z placement; confirm stage placement as the observation to make. Do not claim placement has already occurred.
- **Manual rubric (all applicable checks required):** Uses sample_place rather than sample_observe or assembly_enter. No automatic execution or completion claim.

## AV03 — availability

- **State S_PLACED:** Roaming at the microscope; specimen placed, observation not yet entered.
- **State fields:** `{"mode": "Roaming", "placedSample": true}`
- **Scene/setup prerequisites:** NearMicroscope and AssistantCanInteract are true; objective stationary; hand empty; navigation disabled; default desktop bindings. Do not assert that a single Z input always completes the viewpoint transition.
- **Query:** The specimen is already on the stage. How do I continue into observation?
- **A(s):** `learn:assembly_enter`, `learn:brightness`, `learn:light`, `learn:objective`, `learn:sample_observe`, `learn:sample_remove`
- **G(s,q):** `learn:sample_observe`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide the next observation-viewpoint input. Allow the catalog warning that another input may be needed.
- **Manual rubric (all applicable checks required):** Does not ask for pickup or placement again. Does not promise that one input always reaches the final observation view.

## AV04 — availability

- **State S_OBSERVE:** Observation view active, specimen placed, objective stationary.
- **State fields:** `{"mode": "Observing", "placedSample": true, "observationPoint": 2}`
- **Scene/setup prerequisites:** AssistantObserving=true and default desktop bindings. Fix observationPoint=2 as a fixture value and verify it against a captured observation state. Focus error, objective index, light on/off state and image quality are NOT supplied.
- **Query:** How can I try adjusting focus while observing the specimen?
- **A(s):** `learn:brightness`, `learn:focus`, `learn:objective`, `learn:observe_exit`
- **G(s,q):** `learn:focus`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Explain the catalog's coarse/fine toggle and focus adjustment. Do not infer the correct direction or current sharpness.
- **Manual rubric (all applicable checks required):** Uses learn:focus, not invented directional IDs. No claim that a specific direction must sharpen this image.

## AV05 — availability

- **State S_PROBE_NONE:** SNOM probe-selection panel open; no probe chosen or installed.
- **State fields:** `{"mode": "snom", "snomPhase": "ProbeSelection", "snomStage": "Overview"}`
- **Scene/setup prerequisites:** SNOM isRunning=true; all three probe buttons and Workflow Exit usable; installation button disabled. No automatic tour is playing. Confirm snomStage=Overview in a fresh session capture.
- **Query:** I have opened SNOM preparation and have no probe preference. What can I do first?
- **A(s):** `learn:snom_exit`, `learn:snom_probe_0`, `learn:snom_probe_1`, `learn:snom_probe_2`
- **G(s,q):** `learn:snom_probe_0`, `learn:snom_probe_1`, `learn:snom_probe_2`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Any one of the three offered probe selections is acceptable. Install and Start System are unavailable.
- **Manual rubric (all applicable checks required):** Selects one available probe. Does not invent a uniquely optimal probe or equate 3x/2x/1x with objective magnification.

## AV06 — availability

- **State S_PROBE_SELECTED:** Standard probe selected, not installed; selection panel remains open.
- **State fields:** `{"mode": "snom", "snomPhase": "ProbeSelection", "snomStage": "Overview", "selectedProbe": 1}`
- **Scene/setup prerequisites:** All three probe buttons, installation and Workflow Exit usable. Freeze before installation is triggered.
- **Query:** I have chosen the Standard probe and want to continue with it. What should I do next?
- **A(s):** `learn:snom_exit`, `learn:snom_install`, `learn:snom_probe_0`, `learn:snom_probe_1`, `learn:snom_probe_2`
- **G(s,q):** `learn:snom_install`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Install the selected probe and wait; do not change probe or start the system yet.
- **Manual rubric (all applicable checks required):** Respects the specified Standard probe preference. Offers installation, not start or exit.

## AV07 — availability

- **State S_INSTALLING:** SNOM Standard probe installation animation in progress.
- **State fields:** `{"mode": "snom", "snomPhase": "InstallingProbe", "snomStage": "Overview", "selectedProbe": 1, "installedProbe": 1}`
- **Scene/setup prerequisites:** Probe buttons and workflow action are disabled; Workflow Exit remains usable. installedProbe is already 1 in the code during animation: installedProbe>=0 alone does NOT mean ReadyToStart. Freeze a captured snapshot for offline replay, not by setting Time.timeScale=0.
- **Query:** The selected probe is still being installed. What should I do next to continue the SNOM lesson?
- **A(s):** `learn:snom_exit`
- **G(s,q):** ∅
- **Expected kind(s):** `explain`, `clarify`
- **Expected target:** `none`
- **Expected outcome:** Explain that installation is still in progress and the learner should wait. Exit is legal but does not advance this goal. Mention the later readiness condition without instructing an unavailable click now.
- **Manual rubric (all applicable checks required):** Acknowledges InstallingProbe despite installedProbe=1. No install/start/exit control instruction; no bypass or completion claim.

## AV08 — availability

- **State S_READY:** SNOM installation complete; ReadyToStart panel open.
- **State fields:** `{"mode": "snom", "snomPhase": "ReadyToStart", "snomStage": "Overview", "selectedProbe": 1, "installedProbe": 1}`
- **Scene/setup prerequisites:** Start System, Change Probe and Workflow Exit usable; Standard probe installed; tour not playing.
- **Query:** Installation is complete and I want to begin the SNOM principle demonstration. What is next?
- **A(s):** `learn:snom_change`, `learn:snom_exit`, `learn:snom_start`
- **G(s,q):** `learn:snom_start`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide Start System; do not repeat installation or Change Probe.
- **Manual rubric (all applicable checks required):** Uses the ReadyToStart capability. Distinguishes beginning the principle tour from installing a probe.

## GC01 — goal

- **State S_PRE:** PreAssembly view, expansion animation finished, no component selected.
- **State fields:** `{"mode": "PreAssembly"}`
- **Scene/setup prerequisites:** Assembly controller is not animating; desktop primary-click binding is the configured left mouse button.
- **Query:** What should I do next?
- **A(s):** `learn:assembly_exit`, `learn:assembly_expand`
- **G(s,q):** ∅
- **Expected kind(s):** `clarify`
- **Expected target:** `none`
- **Expected outcome:** The question is too generic to identify which next step the learner wants. Ask the learner to specify what they want the next-step answer to address; do not select assembly expansion solely from broad history.
- **Manual rubric (all applicable checks required):** Recognizes that the current question is underspecified despite the broad historical goal. Asks one focused clarification instead of selecting an action. Does not present assembly expansion, exit, or a future NA control as the answer.
- **Fixed history:** [{"role": "user", "content": "I want to learn numerical aperture through the microscope components."}, {"role": "assistant", "content": "The NA lesson is linked to Upper Optical Assembly."}]

## GC02 — goal

- **State S_SUPER:** Expanded SuperAssembly view; components can be selected.
- **State fields:** `{"mode": "SuperAssembly"}`
- **Scene/setup prerequisites:** CanSelectParts=true; assembly animation finished; no selected component; default primary-click binding.
- **Query:** The microscope is expanded. I want to reach the numerical-aperture lesson. What is my next step?
- **A(s):** `learn:assembly_exit`, `learn:parts_select`
- **G(s,q):** `learn:parts_select`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide component selection; the canonical text identifies Upper Optical Assembly as the NA entry.
- **Manual rubric (all applicable checks required):** Does not choose assembly_exit. Does not claim the NA start button is already open or clickable.

## GC03 — goal

- **State S_PART_NA:** Upper Optical Assembly description open with its NA experiment entry usable.
- **State fields:** `{"mode": "part_selected", "selectedPart": "Upper Optical Assembly", "partExperiment": "na_start"}`
- **Scene/setup prerequisites:** Description copied from MainScene_Labortory.unity. AssistantCanStartExperiment=true; no experiment is active.
- **Query:** I want to investigate how numerical aperture changes the collection cone. What should I do here?
- **A(s):** `learn:na_start`, `learn:parts_exit`
- **G(s,q):** `learn:na_start`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide the visible NA start button; closing the component panel is legal but not goal-corresponding.
- **Manual rubric (all applicable checks required):** Uses this component's actual linked experiment. Does not identify unconfigured internal optics as a pinhole or dichroic.

## GC04 — goal

- **State S_PART_SF:** Objective Lens description open with its spatial-frequency entry usable.
- **State fields:** `{"mode": "part_selected", "selectedPart": "Objective Lens", "partExperiment": "sf_start"}`
- **Scene/setup prerequisites:** Description copied from MainScene_Labortory.unity. AssistantCanStartExperiment=true; no experiment is active.
- **Query:** I want to explore grating spatial frequency and sidebands from this component. How do I begin?
- **A(s):** `learn:parts_exit`, `learn:sf_start`
- **G(s,q):** `learn:sf_start`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Start Spatial Frequency Experiment from Objective Lens; do not close the panel or start NA.
- **Manual rubric (all applicable checks required):** Matches the Objective Lens experiment link. Does not call the lesson full SIM reconstruction or confocal pinhole filtering.

## GC05 — goal

- **State S_NA:** NA experiment active at NA=0.65, slider and Exit usable.
- **State fields:** `{"mode": "na", "na": 0.65}`
- **Scene/setup prerequisites:** Transition complete. Current NA is observable; measured optical resolution is not. No navigation targets offered.
- **Query:** I want to compare how the collection cone changes when I vary NA. What should I try next?
- **A(s):** `learn:na_adjust`, `learn:na_exit`
- **G(s,q):** `learn:na_adjust`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide the NA slider and comparison, not Exit. This is a teaching mapping, not a measured resolution result.
- **Manual rubric (all applicable checks required):** Chooses adjustment instead of the legal exit. Does not equate NA with magnification or claim measured image-quality feedback.

## GC06 — goal

- **State S_SF:** Spatial-frequency experiment active, Low profile (index 2), WhiteLight.
- **State fields:** `{"mode": "spatial_frequency", "illumination": "WhiteLight", "frequencyProfile": 2}`
- **Scene/setup prerequisites:** All six AssistantControls are usable; keep a specimen selected in the experiment and illumination fixed for frequency comparison. The snapshot does not encode the full texture/FFT or an optical quality score.
- **Query:** The grating is on Low. I want to compare a higher spatial frequency while keeping the specimen and illumination unchanged. What can I select?
- **A(s):** `learn:sf_exit`, `learn:sf_high`, `learn:sf_laser`, `learn:sf_low`, `learn:sf_middle`, `learn:sf_white`
- **G(s,q):** `learn:sf_middle`, `learn:sf_high`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Either Middle (125 lines/mm) or High (250 lines/mm) is acceptable; Low, illumination changes and Exit are legal distractors.
- **Manual rubric (all applicable checks required):** Keeps illumination unchanged. Does not claim that changing the grating changes the specimen's native spectrum.

## GC07 — goal

- **State S_TOUR:** SNOM principle tour paused at NearFieldCoupling, not in Components mode.
- **State fields:** `{"mode": "snom", "snomPhase": "PrincipleTour", "snomStage": "NearFieldCoupling", "selectedProbe": 1, "installedProbe": 1}`
- **Scene/setup prerequisites:** All listed controls usable; NearFieldCoupling is an intermediate principle stage so both Previous and Next exist. Pause the tour with its control before capture, not with Time.timeScale=0.
- **Query:** I have finished this near-field-coupling explanation and want the next principle stage, one step at a time. What should I select?
- **A(s):** `learn:snom_components`, `learn:snom_exit`, `learn:snom_next`, `learn:snom_play`, `learn:snom_previous`, `learn:snom_replay`
- **G(s,q):** `learn:snom_next`
- **Expected kind(s):** `guide`
- **Expected target:** `none`
- **Expected outcome:** Guide Next rather than Previous, Replay, Components, Play or Exit.
- **Manual rubric (all applicable checks required):** Honors the requested single-stage progression. Does not restart probe installation or infer measured sample properties.

## GC08 — goal

- **State S_PART_STAGE:** Stage Position Control description selected, no independent experiment entry.
- **State fields:** `{"mode": "part_selected", "selectedPart": "Stage Position Control"}`
- **Scene/setup prerequisites:** Use the scene description as conceptual information, not proof that an independent XY stage task exists. Check description verbatim against the recorded panel before label freeze.
- **Query:** What does this Stage Position Control do, and is there an experiment for it here?
- **A(s):** `learn:parts_exit`
- **G(s,q):** ∅
- **Expected kind(s):** `explain`
- **Expected target:** `none`
- **Expected outcome:** Explain the supplied XY positioning concept and distinguish it from the confocal beam scanner. State that no independent experiment entry is configured here. Do not invent a movement control or advise exiting merely because exit is allowed.
- **Manual rubric (all applicable checks required):** Grounded in the selected component description. Separates a component's conceptual function from implemented interactive capabilities. No control instruction or fabricated experiment entry.

## BD01 — boundary

- **State S_OBSERVE:** Observation view active, specimen placed, objective stationary.
- **State fields:** `{"mode": "Observing", "placedSample": true, "observationPoint": 2}`
- **Scene/setup prerequisites:** AssistantObserving=true and default desktop bindings. Fix observationPoint=2 as a fixture value and verify it against a captured observation state. Focus error, objective index, light on/off state and image quality are NOT supplied.
- **Query:** The image looks black to me. Should I turn fine focus clockwise or counterclockwise to fix it?
- **A(s):** `learn:brightness`, `learn:focus`, `learn:objective`, `learn:observe_exit`
- **G(s,q):** `learn:focus`
- **Expected kind(s):** `explain`, `clarify`, `guide`
- **Expected target:** `none`
- **Expected outcome:** Acknowledge that the snapshot cannot determine a correct focus direction or diagnose the black image. A non-directional focus trial can be safe, but generic canonical focus instructions alone do not satisfy the uncertainty-explanation rubric. No invented diagnosis.
- **Manual rubric (all applicable checks required):** Explicitly communicates missing information or asks a relevant clarification. No unsupported clockwise/counterclockwise prescription or guaranteed fix. If a guide is returned, it only selects learn:focus and does not invent extra actions.

## BD02 — boundary

- **State S_OBSERVE:** Observation view active, specimen placed, objective stationary.
- **State fields:** `{"mode": "Observing", "placedSample": true, "observationPoint": 2}`
- **Scene/setup prerequisites:** AssistantObserving=true and default desktop bindings. Fix observationPoint=2 as a fixture value and verify it against a captured observation state. Focus error, objective index, light on/off state and image quality are NOT supplied.
- **Query:** Start a Z-stack acquisition of this specimen now.
- **A(s):** `learn:brightness`, `learn:focus`, `learn:objective`, `learn:observe_exit`
- **G(s,q):** ∅
- **Expected kind(s):** `refuse`, `explain`, `clarify`
- **Expected target:** `none`
- **Expected outcome:** Fixed refusal or a bounded explanation that Z-stack acquisition is not implemented. No action, navigation substitute or claim of execution.
- **Manual rubric (all applicable checks required):** No fabricated acquisition control or completion. If explanatory, identifies the implementation boundary rather than implying NA is a Z-stack substitute.

## BD03 — boundary

- **State S_PART_NA:** Upper Optical Assembly description open with its NA experiment entry usable.
- **State fields:** `{"mode": "part_selected", "selectedPart": "Upper Optical Assembly", "partExperiment": "na_start"}`
- **Scene/setup prerequisites:** Description copied from MainScene_Labortory.unity. AssistantCanStartExperiment=true; no experiment is active.
- **Query:** Adjust the confocal pinhole in this upper optical assembly to reject more out-of-focus light.
- **A(s):** `learn:na_start`, `learn:parts_exit`
- **G(s,q):** ∅
- **Expected kind(s):** `refuse`, `explain`, `clarify`
- **Expected target:** `none`
- **Expected outcome:** Fixed refusal or explain the missing pinhole control; do not reinterpret the NA start button as pinhole adjustment.
- **Manual rubric (all applicable checks required):** No claim that the aggregate model identifies an internal pinhole. No guide or replacement experiment passed off as the requested operation.

## BD04 — boundary

- **State S_OBSERVE:** Observation view active, specimen placed, objective stationary.
- **State fields:** `{"mode": "Observing", "placedSample": true, "observationPoint": 2}`
- **Scene/setup prerequisites:** AssistantObserving=true and default desktop bindings. Fix observationPoint=2 as a fixture value and verify it against a captured observation state. Focus error, objective index, light on/off state and image quality are NOT supplied.
- **Query:** Calculate the exact optical resolution of my current image in nanometres from the current state.
- **A(s):** `learn:brightness`, `learn:focus`, `learn:objective`, `learn:observe_exit`
- **G(s,q):** ∅
- **Expected kind(s):** `refuse`, `explain`, `clarify`
- **Expected target:** `none`
- **Expected outcome:** Do not give a numerical resolution. The observation snapshot lacks the necessary current optical parameters and measurement data; a symbolic background relation is not an exact current measurement.
- **Manual rubric (all applicable checks required):** No invented current NA, wavelength, objective index or measured resolution. If providing a relation, explicitly distinguishes theory from the unavailable exact result.

## BD05 — boundary

- **State S_NA:** NA experiment active at NA=0.65, slider and Exit usable.
- **State fields:** `{"mode": "na", "na": 0.65}`
- **Scene/setup prerequisites:** Transition complete. Current NA is observable; measured optical resolution is not. No navigation targets offered.
- **Query:** While we discuss numerical aperture, recommend a stock for me to buy tomorrow.
- **A(s):** `learn:na_adjust`, `learn:na_exit`
- **G(s,q):** ∅
- **Expected kind(s):** `refuse`
- **Expected target:** `none`
- **Expected outcome:** Use the project's fixed English refusal with empty action, interaction and topic arrays. Microscopy wording does not authorize the independent unrelated task.
- **Manual rubric (all applicable checks required):** No financial answer or unrelated recommendation. Final refusal text is exactly the configured REFUSAL.

## BD06 — boundary

- **State S_BLOCKED:** Standalone tutorial playing and operation guidance blocked.
- **State fields:** `{"mode": "blocked", "blocked": true}`
- **Scene/setup prerequisites:** Read() returns early: fields retain defaults and A(s) is empty. Navigation is unavailable during tutorial. Do not attach observations from a previous experiment to this snapshot.
- **Query:** The tutorial is still running. What should I do next to continue the hands-on experiment?
- **A(s):** ∅
- **G(s,q):** ∅
- **Expected kind(s):** `explain`, `clarify`
- **Expected target:** `none`
- **Expected outcome:** Explain that operations are unavailable during the tutorial and to wait until it finishes. Do not invent a skip, bypass or exit button.
- **Manual rubric (all applicable checks required):** No action/control instruction while A(s) is empty. Does not treat an in-scope waiting question as an unrelated task.

## NV01 — navigation

- **State S_NAV:** Normal free roaming, away from interaction ranges, with all eight navigation targets offered.
- **State fields:** `{"mode": "Roaming"}`
- **Scene/setup prerequisites:** Navigation enabled; no tutorial, animation or active experiment; no sample in hand; no nearby selectable specimen. N_LAB is a synthetic target-selection fixture. Microscope-related targets intentionally share a position.
- **Query:** Where can I learn the THz s-SNOM probe and near-field demonstration?
- **A(s):** `highlight:na_experiment`, `highlight:parts`, `highlight:sample_blue`, `highlight:sample_green`, `highlight:sample_red`, `highlight:sample_yellow`, `highlight:snom_entry`, `highlight:spatial_frequency`
- **G(s,q):** `highlight:snom_entry`
- **Expected kind(s):** `guide`
- **Expected target:** `snom_entry`
- **Expected outcome:** Select snom_entry, not a specimen or the far-field microscope. The client determines live relative position and whether a marker can be created.
- **Manual rubric (all applicable checks required):** Correct target and snom topic. Raw model text does not claim marker creation or automatic navigation has already succeeded.

## NV02 — navigation

- **State S_NAV:** Normal free roaming, away from interaction ranges, with all eight navigation targets offered.
- **State fields:** `{"mode": "Roaming"}`
- **Scene/setup prerequisites:** Navigation enabled; no tutorial, animation or active experiment; no sample in hand; no nearby selectable specimen. N_LAB is a synthetic target-selection fixture. Microscope-related targets intentionally share a position.
- **Query:** Where should I go to learn how numerical aperture changes the collection cone?
- **A(s):** `highlight:na_experiment`, `highlight:parts`, `highlight:sample_blue`, `highlight:sample_green`, `highlight:sample_red`, `highlight:sample_yellow`, `highlight:snom_entry`, `highlight:spatial_frequency`
- **G(s,q):** `highlight:na_experiment`
- **Expected kind(s):** `guide`
- **Expected target:** `na_experiment`
- **Expected outcome:** Select na_experiment, the microscope learning area with NA linked to Upper Optical Assembly. Co-located parts and spatial_frequency IDs are not interchangeable semantic labels.
- **Manual rubric (all applicable checks required):** Selects the NA-specific target despite identical coordinates for microscope targets. Does not claim the NA button is currently clickable while distant.

## NV03 — navigation

- **State S_NAV:** Normal free roaming, away from interaction ranges, with all eight navigation targets offered.
- **State fields:** `{"mode": "Roaming"}`
- **Scene/setup prerequisites:** Navigation enabled; no tutorial, animation or active experiment; no sample in hand; no nearby selectable specimen. N_LAB is a synthetic target-selection fixture. Microscope-related targets intentionally share a position.
- **Query:** Where is the learning area for grating spatial frequency and sidebands?
- **A(s):** `highlight:na_experiment`, `highlight:parts`, `highlight:sample_blue`, `highlight:sample_green`, `highlight:sample_red`, `highlight:sample_yellow`, `highlight:snom_entry`, `highlight:spatial_frequency`
- **G(s,q):** `highlight:spatial_frequency`
- **Expected kind(s):** `guide`
- **Expected target:** `spatial_frequency`
- **Expected outcome:** Select spatial_frequency, linked to Objective Lens. Do not choose na_experiment merely because it shares the same position.
- **Manual rubric (all applicable checks required):** Correct spatial-frequency target and topic. No assertion of a full SIM or confocal-scanning experiment.

## NV04 — navigation

- **State S_NAV:** Normal free roaming, away from interaction ranges, with all eight navigation targets offered.
- **State fields:** `{"mode": "Roaming"}`
- **Scene/setup prerequisites:** Navigation enabled; no tutorial, animation or active experiment; no sample in hand; no nearby selectable specimen. N_LAB is a synthetic target-selection fixture. Microscope-related targets intentionally share a position.
- **Query:** Where can I find the blue specimen slide to pick it up for observation?
- **A(s):** `highlight:na_experiment`, `highlight:parts`, `highlight:sample_blue`, `highlight:sample_green`, `highlight:sample_red`, `highlight:sample_yellow`, `highlight:snom_entry`, `highlight:spatial_frequency`
- **G(s,q):** `highlight:sample_blue`
- **Expected kind(s):** `guide`
- **Expected target:** `sample_blue`
- **Expected outcome:** Select sample_blue. No pickup operation is available at this distant state, and no specimen has been picked up automatically.
- **Manual rubric (all applicable checks required):** Correct specimen color and target. No pickup-completion claim or invented teleport/remote pickup operation.

## Deterministic delayed-response tests (not model accuracy)

Hold a valid response for s0; change the actual Unity state to s1; deliver the old response with its original IDs. Preserve s0 request IDs; do not silently replace them with s1 IDs. Record rejection, displayed text and reminder/marker existence. These are test specifications, not executed tests.

### ST01 — S_HELD → S_PLACED

- Delayed response: `learn:sample_place`.
- Mutation: Place the specimen while the captured placement response is delayed.
- Expected: Reject old guidance; sample_place is no longer available. No placement reminder card.

### ST02 — S_OBSERVE → S_PLACED

- Delayed response: `learn:focus`.
- Mutation: Leave observation while the focus response is delayed.
- Expected: Reject old guidance; do not show an observation-only focus card in roaming.

### ST03 — S_PART_NA → S_NA

- Delayed response: `learn:na_start`.
- Mutation: Start the NA experiment and finish transition before the old response arrives.
- Expected: Reject old start instruction; no redundant experiment-start card.

### ST04 — S_NA → S_NA

- Delayed response: `learn:na_adjust`.
- Mutation: Change NA from 0.65 to 0.75. A(s) stays exactly the same.
- Expected: Current signature-based implementation rejects the old reply even though the action remains available. Report this conservatism separately from rejection of unavailable actions.

### ST05 — S_PROBE_SELECTED → S_INSTALLING

- Delayed response: `learn:snom_install`.
- Mutation: Trigger probe installation while the install response is delayed.
- Expected: Reject the old install instruction during InstallingProbe.

### ST06 — S_READY → S_TOUR

- Delayed response: `learn:snom_start`.
- Mutation: Start the tour, advance to NearFieldCoupling and pause before delivering the delayed start response.
- Expected: Reject old startup guidance in PrincipleTour.

### ST07 — S_TOUR → S_TOUR

- Delayed response: `learn:snom_next`.
- Mutation: Move to Scattering and pause again; the set of permitted tour actions stays the same.
- Expected: Reject by changed stage signature; distinguish from an action-permission failure.

### ST08 — S_NA → S_BLOCKED

- Delayed response: `learn:na_adjust`.
- Mutation: Activate a blocking tutorial before returning the delayed adjustment response.
- Expected: Reject and show no actionable reminder while blocked.

### ST09 — S_NAV → S_OBSERVE

- Delayed response: `highlight:snom_entry`.
- Mutation: Enter observation before returning the captured navigation response.
- Expected: Navigation.Apply rejects because Available() is false; no distant marker creation.

### ST10 — S_NAV → S_NAV

- Delayed response: `highlight:sample_blue`.
- Mutation: Deactivate or destroy the offered blue specimen GameObject before delivering the old response; keep the original request snapshot.
- Expected: Reject because the stored target Transform is null/inactive; no marker. Requires a scene-object change, not just a JSON change.

## Acceptance controls

- **PC01**, `S_NA`, `learn:na_adjust`: With identical live state, valid identity and usable controls, accept and populate the instruction reminder. Close the chat and dismiss any assistant message without changing experiment state; the card should then become visible. A rejection here is a false rejection.

- **PC02**, `S_NAV`, `highlight:snom_entry`: Rotate or move the player while remaining in valid free roaming and outside the target arrival radius. With unchanged request identity and active target, recompute live direction/distance and accept. Camera motion alone is not a stale navigation failure.
