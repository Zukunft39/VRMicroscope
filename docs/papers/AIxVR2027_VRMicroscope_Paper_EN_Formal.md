# VRMicroscope: Integrating Interactive Microscopy Simulation with Task-State-Aware AI Guidance in Virtual Reality

**Authors and Affiliations:** Anonymous for review


> **[EDITORIAL NOTE — REMOVE BEFORE SUBMISSION]** Figure placeholders, study-record checks, and the supplementary evaluation protocol are editorial annotations, not completed figures or new results. Figures 1–4 are prioritized; optional Figure 5 requires data. Layouts, captions, and prior-paper examples are documented in the [figure plan](AIxVR2027_Figure_Plan.md).

## Abstract

Microscopy education requires learners to connect instrument structure and operating procedures with changes in observable results, yet access to advanced instruments is often constrained by limited training time, operational risk, and teaching resources. This paper presents VRMicroscope, a virtual-reality learning system that integrates interactive microscopy simulation with task-state-aware AI guidance. The VR environment supports specimen pickup and placement, observation viewpoints, coarse and fine focusing, illumination and objective adjustment, and exploded component inspection. Linked modules illustrate numerical aperture, spatial frequency and structured illumination, and the basic workflow of terahertz scattering-type scanning near-field optical microscopy (THz s-SNOM). The system explicitly distinguishes instructional visual mappings and simplified numerical models from calibrated optical measurement. The AI assistant combines project knowledge, current interface state, and a catalog of available actions: a large language model generates explanations or candidate guidance, while application code validates action identifiers and rejects recommendations that no longer match the current state. Spatial navigation creates target markers from live scene geometry, and Chinese/English speech input produces editable text before submission. Evaluation combines implementation-level conformance checks with an exploratory user study. Eighteen of 19 participants completed the core task sequence (94.7%), and the whole-sample mean System Usability Scale (SUS) score was 82.4/100. Descriptive NASA-TLX results showed that Effort (44.7) and Mental Demand (42.9) were the largest reported workload dimensions, whereas mean Frustration was 24.2. Because the study used a single-group exploratory design, these outcomes are interpreted as evidence of workflow feasibility and perceived usability rather than as causal evidence of learning gains attributable to AI. The main contribution is an integrated microscopy-learning environment in which physical interaction, scientifically bounded instructional simulation, and conversational guidance share an explicit representation of what the learner can currently do.

**Keywords:** virtual reality; microscopy education; interactive simulation; task-state awareness; large language models; speech input.

## 1. Introduction

Microscopy training requires learners not only to identify three-dimensional instrument components, but also to place specimens, adjust focus and illumination, switch objectives, and explain why the observed field changes. A virtual laboratory can provide a repeatable environment for exploring these operations, but it must make clear which feedback corresponds to real principles and which feedback is designed primarily for instructional purposes.

VRMicroscope addresses this problem through two complementary layers. The experimental environment provides objects, controls, visual feedback, and experiment interfaces, while the AI assistant connects learners' questions about these experiences to project knowledge and currently accessible learning activities. For example, when a user is viewing the upper optical assembly, the user can first ask about its function and then enter the associated numerical-aperture experiment. If the user is still elsewhere in the room, spatial guidance is needed first.

The central system challenge is therefore to maintain consistency among the instrument interface, the scientific boundaries of the simulation, and conversational recommendations. A description of a real confocal instrument may involve pinhole adjustment or Z-stack acquisition, but the present project does not provide those confirmed operations. Conversely, if the assistant only knows that “the project includes an NA experiment” but does not know that the experiment-entry control is already visible in the current component panel, it may incorrectly instruct the user to exit the view.

The main contributions of this work are:

1. We build a microscopy experimental environment that connects specimen manipulation, focus and illumination feedback, structural understanding, and three advanced instructional modules, while explicitly stating the inputs, outputs, and modeling boundaries of each activity.
2. We design an AI guidance architecture that combines project knowledge, current component descriptions, an available-action catalog, response validation, spatial markers, and confirmation-based speech input.
3. We evaluate the system through implementation-level conformance checks and an exploratory study with 19 participants, reporting task completion, perceived usability, and subjective workload without interpreting these measures as untested causal learning gains.

The system is intended for operational familiarization and conceptual exploration. Transfer to real instruments, quantitative imaging capability, and any additional learning benefit attributable to AI require corresponding evaluation evidence.

## 2. Related Work and Research Positioning

Recent work on generative AI for immersive education has moved beyond generic chat interfaces toward assistants tied to concrete learning activities. In an AIxVR 2024 anatomy study, Chheang et al. compared virtual-assistant configurations while reporting task performance, SUS, NASA-TLX, presence, and interview feedback as distinct outcome categories [1]. Geris and Alce instead evaluated a voice-based XR tutoring prototype at AIxVR 2026 through structured interaction sessions and automatically logged measures of latency, response duration, and operating cost [2]. At the same conference, SpatialTutor combined object-aware mixed-reality training, spatial guidance, and LLM-based support for procedural medical skills [3]. Together, these studies illustrate an important evaluation principle for AI+XR systems: interaction performance, task outcomes, subjective experience, and learning effects should be measured separately rather than used as substitutes for one another.

The present work focuses on the correspondence between microscopy learning activities and guidance. An operation must be genuinely available in the current interface, and its resulting feedback must be explained within an explicit scientific boundary of the simulation. We therefore treat the AI as a constrained tutoring layer embedded in the experimental environment rather than as an autonomous instrument-controlling agent, and we report implementation-level constraint behavior separately from user-level usability outcomes.

Quantitative confocal microscopy provides an important scientific reference point [4]. The present project introduces related principles but does not implement a calibrated confocal acquisition pipeline. Educational resources on numerical aperture and spatial frequency [5], [6] inform the conceptual presentation of the modules; following these principles does not imply that every graphical mapping in the system has been validated against physical measurements.

## 3. VR Experimental Environment and Simulation Boundaries

### 3.1 Laboratory Organization and Interaction States

The Unity laboratory supports both desktop and XR interaction. The main interaction states are `Roaming`, `Observing`, and `Tutorial`; component viewing and individual experiment controllers maintain additional local states. Viewpoints, object activation, input permissions, and interface visibility jointly determine which learning activities are currently available to the user.

Main-state transitions are deferred to frame boundaries, while tutorial input restrictions are handled by separate checks. These mechanisms organize transitions and reduce competing responses to a single input. Opening the AI dialog also blocks ordinary experimental operations, so operation instructions require the user to close the dialog before acting.

The overall learning content can be connected as follows: handle a specimen, observe an image, adjust the instrument, inspect components, and enter associated experiments. This is an exploratory learning path and does not mean that every user must complete the same mandatory tutorial sequence.

> **[FIGURE 1 PLACEHOLDER — PRIORITY; TWO COLUMNS RECOMMENDED]** Use a runtime laboratory overview to identify the microscope station, SNOM apparatus, and specimen locations. Add three crops for specimen observation, exploded components, and SNOM probe installation. Label activities A–D and retain one view of the assistant entry point. Exclude editor chrome and unrelated panels.
>
> **Proposed caption:** Fig. 1. VRMicroscope learning environment and core activities. Learners handle specimens, observe microscope feedback, inspect components, and explore the SNOM workflow within one laboratory, with access to contextual assistant guidance.

**Table 1. Inputs, outputs, and instructional boundaries of the activities currently implemented in the project.**

| Activity and player input | Actual output | Intended instructional connection | Modeling boundary |
|---|---|---|---|
| Pick up, place, observe, and remove a specimen | Changes to the runtime specimen's parent object, pose, visibility, and observation environment | Connect specimen placement with instrument observation | Scripted object manipulation; contact mechanics have not been validated |
| Coarse/fine focusing and objective switching | Visibility, transparency, rendering feedback, and field-of-view size vary with the focus parameter | Search for an observable range first, then refine the adjustment | Manually defined focus ranges; no confocal point-spread-function calculation |
| Adjust illumination | Changes to light-cone line width and the brightness input to the specimen shader | Compare illumination settings with observation results | Dimensionless display gain, not calibrated illuminance |
| SuperAssembly and component selection | Exploded layout, component descriptions, and associated experiment buttons | Connect structure, function, and learning activities | Illustrative separation; does not evaluate real mechanical assembly |
| Adjust numerical aperture | Illustrative changes in light-cone geometry and specimen appearance | Explore collection angle and NA | Fixed-medium instructional model; NA does not uniquely determine magnification |
| Select spatial frequency and illumination | Grating/diffraction illustration and a Fourier display derived from the specimen texture | Connect periodic structure, diffraction, and spectral support | Simplified structured-illumination explanation |
| Select an SNOM probe and control the demonstration | Installation, tapping, scanning animations, and illustrative images | Understand the workflow of near-field microscopy | Procedural demonstration using synthetic outputs |

### 3.2 Specimen Manipulation and Observation

When a specimen is picked up, the program creates a runtime copy attached to the user's hand. Placement checks both distance and whether the object actually held by the user carries the required tag, then moves the specimen into the microscope observation environment while standardizing its pose and scale. Removing the specimen restores it as an active hand-held object; when necessary, the program recomputes focus-dependent visibility.

Specimen ownership, visibility, and user observation mode are maintained separately. A placed specimen may be hidden because the focus parameter falls outside its valid range; being hidden does not mean that the specimen has disappeared or returned to the specimen cabinet. `Observing` describes interaction and viewpoint state, which combines with hand-held or placed status to determine observation behavior.

The rendering setup uses separate scene/observation viewpoint objects and a microscopic image camera, with the observation content displayed on the instrument screen through a render texture. Switching objectives changes the orthographic size of the image camera. This structure provides a separate instrument-observation view within the laboratory.

### 3.3 Focus, Objectives, and Illumination

The focusing activity uses coarse adjustment to find an observable specimen range and fine adjustment to refine the display. Let $v$ be the dimensionless slider value and $j$ the objective index. For each objective, $f_j$ is the focus center, $r_j$ the visibility half-width, and $c_j$ the full-opacity half-width. With $e_j=|v-f_j|$, the specimen is active within $e_j\leq r_j$, with opacity

$$
\alpha_j(v)=
\begin{cases}
1,&e_j<c_j,\\
1-\dfrac{e_j-c_j}{r_j-c_j},&c_j\leq e_j\leq r_j.
\end{cases}
$$

Outside this interval the specimen is hidden; within it, a separate rendering-focus parameter is set to $300e_j/r_j$. The four configured objective labels are 5, 10, 50, and 100, with visibility half-widths of 0.17, 0.13, 0.07, and 0.04. Narrower intervals require finer adjustment. The configured coarse-to-fine slider-increment ratio is 25:1. These values define visual feedback and interaction sensitivity rather than calibrated displacement or confocal pinhole transmission.

Objective switching rotates the turret and changes the image camera's orthographic size, allowing learners to compare viewing scales. Illumination adjustment changes the light-line width and a dimensionless shader brightness input. These separate controls support comparison of specimen visibility, apparent scale, and brightness. Focus centers, display gain, and camera sizes are software configuration parameters; physical calibration would require a separate evaluation.

### 3.4 Structural Exploration and Tutorial Constraints

SuperAssembly separates parts according to their centers relative to the model center, with configurable displacement and direction. Normal and exploded representations are switched by mode. Learners select parts in the expanded arrangement to access names, functional descriptions, and associated experiments.

The upper optical assembly links to numerical aperture, while the objective links to spatial frequency. Both belong to the same microscope station. The selected name, description, and experiment entry also form AI-readable context, allowing a component question and a request for hands-on study to continue within the same interface.

Tutorial input conditions and step checks regulate progression. Completion records indicate that required operations have been satisfied; conceptual understanding requires independent learning measures.

### 3.5 Numerical-Aperture Experiment

The NA activity provides a slider range of 0.03–0.95 and uses a fixed refractive index $n=1$:

$$
\mathrm{NA}=n\sin\theta,\qquad \theta=\arcsin(\mathrm{NA}/n).
$$

Here $\theta$ is the collection-cone half-angle. The interface updates the half-angle, full aperture angle, and cone geometry together. The relationship follows microscopy theory [7], with presentation informed by light-cone teaching material [5]. Comparing settings connects a numerical input to a visible geometrical change. Reference magnification anchors and specimen appearance provide illustrative context; NA and magnification remain distinct properties, and the appearance changes are not quantitative brightness, depth-of-field, or resolution measurements.

### 3.6 Spatial Frequency and Structured-Illumination Displays

The spatial-frequency interface offers gratings of 250, 125, and 62.5 lines/mm and illumination choices. For grating frequency $\nu_g$, illustrative wavelength $\lambda$, and illustrative focal length $f_{\mathrm{obj}}$, the diagram uses

$$
D=\frac{1}{\nu_g},\qquad
\sin\psi=\frac{\lambda}{D},\qquad
\rho=f_{\mathrm{obj}}\tan\psi.
$$

The symbol $\rho$ denotes the back-focal-plane offset, distinct from the specimen spectrum below. At small angles, $\rho/f_{\mathrm{obj}}\simeq\lambda/D$, linking grating spacing to diffraction position [6], [8], [9].

A specimen texture is converted into a scalar field. For a selected specimen, a separable Hann-type window reduces boundary effects [10], multiplication by $(-1)^{x+y}$ centers zero frequency, and row/column radix-2 FFTs compute the spectrum [11]. The default size is $128\times128$, with supported power-of-two sizes from 64 to 256. Log compression, normalization, and enhancement make spectral structure visible.

Let $O(\mathbf{k})$ be the specimen spectrum, $\mathbf{k}_0$ the illumination carrier, $\mu$ the modulation depth, and $\varphi$ the illumination phase. The standard SIM background is summarized by [12], [13]

$$
G_{\varphi}(\mathbf{k})=
H_{\mathrm{opt}}(\mathbf{k})
\left[
O(\mathbf{k})+
\frac{\mu}{2}e^{i\varphi}O(\mathbf{k}-\mathbf{k}_0)+
\frac{\mu}{2}e^{-i\varphi}O(\mathbf{k}+\mathbf{k}_0)
\right].
$$

Here $H_{\mathrm{opt}}$ is the theoretical optical transfer function. The current mixed-spectrum panel illustrates only one shifted sideband pair. Its magnitude before display mapping is

$$
B(\mathbf{k})=
P_{\mathrm{demo}}(\mathbf{k})
\left|
\frac{\mu}{2}
\left[O(\mathbf{k}-\mathbf{k}_0)+O(\mathbf{k}+\mathbf{k}_0)\right]
\right|.
$$

$P_{\mathrm{demo}}$ is a nonnegative, implementation-defined pupil weight rather than a measured $H_{\mathrm{opt}}$. Further display mapping is applied to $B$; the panel does not display complex values directly. The recovered-support panel combines central and shifted coverage at 0°, 60°, and 120° and applies it to the display-processed specimen-spectrum magnitude. Together, the panels distinguish spectral translation from multi-orientation coverage without performing complete multiphase acquisition, phase separation, or quantitative reconstruction. Changing the carrier changes sidebands and represented support, not the native spectrum of an unchanged specimen.

> **[FIGURE 2 PLACEHOLDER — PRIORITY; TWO COLUMNS RECOMMENDED]** Use a three-row comparison: (a) two focus states with objective, specimen, and illumination held fixed; (b) two NA settings and their cones, retaining the actual displayed parameter values; (c) two carrier settings for the same specimen, showing source spectrum, mixed sidebands, and recovered support. Use runtime captures and identify the changed input in each comparison. Label synthetic displays accurately. If space is limited, retain the NA and spectrum rows.
>
> **Proposed caption:** Fig. 2. Mapping instructional inputs to observable feedback. Focus, NA, and illumination carrier affect specimen display, cone geometry, and sideband/support displays, respectively. The feedback combines authored mappings and simplified numerical visualization.

### 3.7 THz s-SNOM Demonstration

The SNOM module implements probe selection, installation, and system startup, followed by stages illustrating THz generation and propagation, tapping, near-field coupling, background suppression, and raster scanning. THz time-domain spectroscopy and near-field microscopy provide the theoretical background [14]–[17].

Periodic probe animation, a scan cursor, illustrative waveforms, and surface/near-field images connect stages to observable outputs. The higher-harmonic content explains extracting scattered-signal components at integer multiples of the tapping frequency to help separate near-field contributions from background [16], [17]. The interface illustrates this concept rather than demodulating measured signals.

Probe labels of 3×, 2×, and 1× indicate relative instructional detail. Program-generated waveforms and images support procedural and conceptual exploration; measured arbitrary-pixel traces, experimental amplitude/phase inversion, and dielectric-property retrieval are outside this module. Model provenance and component correspondence should be reconciled with the authors' project records.

## 4. AI Guidance Integrated with the Laboratory

### 4.1 Conversation Entry and Knowledge Flow

A corner-mounted animated assistant provides access to questions, while local greetings and idle suggestions introduce learning topics. Explicit questions are sent through a Python gateway to DeepSeek. The gateway combines authored project knowledge, interaction descriptions, and current snapshots, supplying component and activity context without granting the model access to project files or the rendered view.

The system injects a fixed knowledge bundle alongside a shared action catalog. Knowledge supports conceptual explanations; the catalog specifies operating steps, linked by the current experimental state. Prompt and knowledge versions accompany responses to associate future records with the deployed configuration. Speech transcription is a separate entry path: audio becomes an editable draft before user confirmation initiates the same question-answering pipeline.

> **[FIGURE 3 PLACEHOLDER — PRIORITY; TWO COLUMNS RECOMMENDED]** Draw an original vector architecture diagram with Unity, Python gateway, and external API regions. Show question/current snapshots → candidate generation → action/schema checks → canonical instructions → semantic review → client freshness checks → reminder or marker. Include at most one format-repair loop back to generation. The speech branch is microphone → Groq transcription → editable draft → user confirmation. Close the loop through learner action and VR state update; do not draw the model directly operating the instrument.
>
> **Proposed caption:** Fig. 3. State-constrained question-answering architecture. The model selects candidate explanations or guidance, while application code checks availability, renders operating instructions, and verifies freshness. Format repair retains the same permissions; learners execute experimental operations.

### 4.2 Task State and Available Activities

**Table 2. Correspondence between VR activities and AI context.**

| VR context | Supplied information | Supported guidance |
|---|---|---|
| Specimen handling | Hand object, placement state, available actions | Pickup, placement, observation, or removal |
| Microscope observation | Mode, observation point, available controls | Focus, brightness, or objective instructions |
| Component inspection | Name, description, linked experiment, usable start action | Component explanation and experiment entry |
| NA/spatial frequency | Current module parameters and controls | Parameter interpretation and available steps |
| SNOM | Stage, probe state, playback, component mode | Workflow and prerequisites |
| Navigation | Offered targets, positions, player heading, scale | Instrument-area or specific-specimen markers |

The snapshot excludes focus-slider values, focus error, current objective index, measured image quality, and coarse/fine selection. Focusing guidance therefore supplies a control method and observation goal, leaving comparison of display changes to the learner; it cannot calculate a direction guaranteed to improve focus. Default fields outside the active module are not treated as measurements.

### 4.3 Constrained Generation and Bounded Format Repair

For shared catalog $\mathcal{C}$ and action-availability predicate $P(a,s)$ in state $s$,

$$
\mathcal{A}(s)=\{a\in\mathcal{C}\mid P(a,s)\}.
$$

Unity constructs $\mathcal{A}(s)$ from interface controls, controller state, proximity, and input bindings. DeepSeek returns an explanation, guidance, clarification, or refusal. Operating guidance selects one permitted action; location guidance selects one offered target. The gateway validates action, interaction, and topic correspondence, and the client validates again before display.

For valid operating actions, catalog instructions and observation prompts replace generated prose. Length or arrow formatting in text that will be discarded therefore does not invalidate an otherwise permitted operation. When candidate JSON, fields, or action selection fail structural validation, the system permits one regeneration from the original question and the same snapshot. All permissions remain enforced after regeneration. Timeouts and upstream access failures are not retried as formatting errors.

Non-refusal candidates then undergo a semantic review of scope, factual support, and correspondence to the user's goal. Generation and review use the same model configuration, making review an additional check rather than an independent correctness guarantee. Generation-format errors, review-format errors, access failures, and timeouts produce distinct service messages; semantic refusals remain separate from system failures.

### 4.4 Learning Continuity and Response Freshness

In a component panel, “this component” is resolved from the selected name and description. Requests for hands-on study prioritize the available associated experiment. A generic next-step request prioritizes progression, such as expansion after pre-assembly or component selection after expansion. Exit is reserved for an explicit exit request, module switching, or a necessary prerequisite route.

Each request stores serialized state and a snapshot identifier. When operating guidance arrives, the client rereads state and compares identity, content, and action permissions. Stale recommendations are not displayed as current instructions. After the dialog closes, a reminder can persist while periodically checking its context. Learners retain control of actual operations; displaying a reminder does not indicate completion.

### 4.5 Instrument and Specimen Localization

Under suitable free-observation conditions, component, NA, and spatial-frequency topics share the microscope station, while SNOM uses a separate apparatus. The updated catalog also includes red, green, blue, and yellow specimen targets. Only objects offered in the current snapshot can be marked. Specimen objects are distinct from a cabinet: this is object localization, not arbitrary furniture search or route planning.

For horizontal target displacement $\Delta_h$ and $u$ world units per meter, displayed distance is

$$
d=\|\Delta_h\|/u.
$$

The signed angle $\beta$ relative to horizontal viewing direction is discretized into front ($|\beta|\leq45^\circ$), back ($|\beta|\geq135^\circ$), and the remaining left/right intervals. Backend geometry supplies the relation, and the client recomputes it when applying the response.

Highlighting is confirmed only after successful creation of the outline and beacon. Display distance is measured to the bounds center; arrival uses horizontal distance to the nearest point on the bounds, with a default threshold of 1.2 m. Approach, cancellation, expiry, or incompatible state changes clear the marker. Learners use the straight-line relation while choosing a traversable route themselves.

### 4.6 User-Confirmed Chinese/English Speech Input

Clicking the speech option starts recording through the default microphone; clicking again stops it, with a 30-second limit. The client converts recorded samples to mono PCM16 WAV, preferentially at a 16-kHz device rate, and uploads the complete clip for Groq `whisper-large-v3-turbo` transcription [18].

No translation is requested. The transcript enters an editable draft for explicit user submission. This permits correction of microscopy terms and routes speech-originated questions through the same state and validation pipeline. Responses are currently textual. Microphone deployment, mixed-language accuracy, and end-to-end waiting time require separate measurements; the evaluated setup must specify PC-VR versus standalone operation and the gateway connection.


## 5. Integrated Learning Scenarios

### 5.1 Specimen Observation and Focus Guidance under Limited State Access

The user picks up a specimen, approaches the microscope, places the specimen, and enters observation. The user then switches objectives, compares fields of view, and uses coarse and fine adjustment to search for a suitable observation range. When help is requested, the assistant can select a currently available focus-adjustment action and explain what changes the learner should observe after performing it.

This connects real controls and instructional prompts that exist in the project. It does not imply that the AI has read the focus error, identified the cause of a black field, or calculated the correct adjustment direction. The learner's own observation of the resulting changes supplies image information that is not included in the state snapshot.

### 5.2 Moving from a Component Question into an Experiment

In SuperAssembly, the user selects the upper optical assembly and asks about its purpose. The assistant uses the panel description to explain its relationship to NA. When the user then asks to try the experiment, the assistant selects the currently available NA launch action. The user closes the dialog, presses the button, and then adjusts the slider to compare changes in the light cone.

Selecting the objective corresponds to the spatial-frequency experiment entry. If the user is still elsewhere in the room, the user first receives a marker for the same microscope learning area rather than being directed to a nonexistent standalone Fourier-experiment bench.


> **[FIGURE 4 PLACEHOLDER — PRIORITY; TWO COLUMNS, FOUR PANELS]** Capture one actual session: (a) a question about the selected component; (b) an explanation based on its panel; (c) a request to try the associated experiment and the returned entry instruction; (d) the learner closes the dialog, starts the experiment, and sees parameter/cone feedback. Connect panels sequentially and retain short authentic dialogue excerpts. Label mock captures as protocol demonstrations, not live-model quality evidence.
>
> **Proposed caption:** Fig. 4. Continuous learning from component explanation to experimental interaction. The assistant uses the current panel to explain the component and identify an available entry; the learner starts the experiment and observes feedback. The sequence illustrates interaction, not learning gains by itself.

### 5.3 SNOM Prerequisites and Response Changes

During probe selection, the assistant can suggest a permitted selection or installation action. While installation is in progress, it can explain that the user should wait or provide a currently available exit action, rather than inventing a shortcut to start scanning immediately. Only after installation is complete does the system-start step become available.

The user can then explore tapping, coupling, and scanning through the demonstration controls. If an operation recommendation returned by the assistant no longer matches the state captured at request time, the client rejects it. This scenario illustrates how procedural conditions are connected to AI guidance; by itself, it does not constitute experimental evidence of adversarial robustness.

## 6. Evaluation Method and Results

To keep evaluation measures aligned with the paper's contributions, we separate implementation-level conformance checks from an exploratory user study. The former asks whether the system behaves according to the state constraints described in the architecture, whereas the latter asks whether learners can complete the core workflow and how they perceive usability and workload. This separation is consistent with prior AIxVR educational XR studies that report objective task performance and subjective questionnaires as distinct evidence [1], and it avoids treating code behavior, interaction performance, and learning outcomes as interchangeable.

### 6.1 Evaluation Questions and Implementation-Level Checks

For reporting, we organize the user study around two evaluation questions:

- **RQ1 (workflow feasibility):** Can participants complete the core VR activities involving specimen handling, microscope observation, focus/illumination adjustment, and structural exploration?
- **RQ2 (perceived experience):** How do participants rate the system's overall usability and subjective workload?

Implementation-level checks are not treated as a third user-study outcome. They verify that the implementation matches the mechanisms described earlier in the paper: constructing the set of currently available actions from interface state; rejecting action identifiers that are absent from the catalog or unavailable in the current state; rejecting guidance after the experiment state has changed since the request; and computing spatial direction and distance from the live player and target geometry. These are deterministic conformance properties, so we do not re-label them as "AI accuracy" or "safety success rates." Frame rate, speech latency, and model response latency are likewise outside the outcome set of the present user study.

### 6.2 Participants and Procedure

> **[STUDY RECORD CHECK — COMPLETE AND REMOVE BEFORE SUBMISSION]** The authors confirm that the study exists and that original materials are outside the repository. Specify the evaluated build/date, PC-VR or standalone/gateway setup, recruitment and prior experience, task success/timeout rules, researcher assistance, and actual AI/speech use. Reconcile the retained summaries with questionnaires and logs. Absence from the repository does not establish loss of original records.

The study included 19 participants. Academic background was not an experimentally manipulated factor; it is therefore reported only as sample composition rather than used to rank participant groups or infer optics expertise.

**Table 3. Academic background of participants ($N=19$).**

| Academic background | $n$ |
|---|---:|
| Physics | 6 |
| Software Engineering | 10 |
| Business | 2 |
| Philosophy | 1 |

Sessions were conducted indoors in a quiet environment using a Meta Quest 3. Participants first received approximately three minutes to become familiar with the controllers and then completed six classes of activities: picking up a specimen; placing it on the microscope and entering observation; using coarse and fine focus adjustment; switching objectives and refocusing/adjusting illumination; leaving observation and exploring SuperAssembly; and using the AI question-answering interface when explanation or operational help was needed. AI questioning was an optional support channel rather than a separate experimental condition; the procedure therefore does not identify a causal difference between "AI" and "no-AI" training.

After the tasks, participants completed the System Usability Scale (SUS) [19] and NASA Task Load Index (NASA-TLX) [20], followed by an approximately 10-minute semi-structured interview. The quantitative analysis below uses task completion and the standardized questionnaires. Interview responses are treated as formative feedback rather than as coded qualitative outcomes, so we do not report theme frequencies or participant percentages from them.

### 6.3 Measures and Analysis

Core-task completion is the proportion of participants completing the required sequence. SUS measures perceived usability [19], and NASA-TLX characterizes subjective workload [20]. This draft retains completion counts and questionnaire means as descriptive results. Academic background describes the sample rather than defining small comparative groups.

> **[STATISTICAL CHECK — COMPLETE BEFORE SUBMISSION]** Use the authors' individual questionnaires to verify means, valid sample sizes, and missing items, and calculate supported SDs or confidence intervals. Establish the Performance scale direction and raw versus weighted TLX scoring; Table 4 temporarily omits that dimension and composite TLX. If completion-time records are available, define handling of incomplete cases and censoring before analysis. Describe missing-record limitations only if the relevant records genuinely cannot be obtained.

Usability and learning-related outcomes are reported separately, consistent with distinctions in educational XR evaluation [21]. Empirical SUS research informs score interpretation [22]; SUS is not a knowledge score.


### 6.4 User-Study Results

**Table 4. Descriptive outcomes for the full sample.**

| Measure | Result |
|---|---:|
| Core-task completion | 18/19 (94.7%) |
| SUS (0–100) | 82.4 |
| NASA-TLX: Mental Demand | 42.9 |
| NASA-TLX: Physical Demand | 27.9 |
| NASA-TLX: Temporal Demand | 30.8 |
| NASA-TLX: Effort | 44.7 |
| NASA-TLX: Frustration | 24.2 |

Eighteen participants completed the core task sequence, indicating that most participants were able to progress through the main interaction flow from specimen handling to microscope observation and structural exploration under the study conditions. This measure supports workflow feasibility; it does not indicate mastery of the underlying optics.

The whole-sample SUS mean was 82.4. In the context of established empirical SUS benchmarks, this result is consistent with high perceived usability [22].

For NASA-TLX, Effort (44.7) and Mental Demand (42.9) were the two largest reported dimensions, while Physical Demand (27.9), Temporal Demand (30.8), and Frustration (24.2) were lower. Because the study did not include a control condition or prespecified hypotheses for individual workload dimensions, we report this pattern descriptively and do not test or causally interpret differences among the TLX dimensions.

### 6.5 Interpretation and Validity Boundaries

This study is a single-group exploratory evaluation. It supports the claims that participants were generally able to complete the core VR microscopy workflow and that the system received a high usability rating. It does not support causal claims that the AI improved learning, reduced workload, or can substitute for training on a real instrument. SUS and NASA-TLX are subjective experience measures and do not replace pre/post optics tests, transfer tests on real equipment, or expert-rated procedural performance.

Academic background is used only to describe the sample. Because the categories are uneven and the Business and Philosophy groups are very small, the paper does not treat them as comparative statistical cohorts. Speech-recognition accuracy, end-to-end latency, frame rate, and robustness to abnormal or adversarial requests require separately designed instrumented benchmarks or stress tests; they are not inferred from SUS or NASA-TLX.

### 6.6 Supplementary Protocol for AI Guidance Quality

> **[PROPOSED EVALUATION — NOT AN EXISTING RESULT]** This section specifies a recommended live-model protocol. Replace it with the actual configuration, counts, and results after execution. If it is not run before submission, move the protocol to future work and remove the Figure 5 placeholder; do not claim evaluated AI quality in the abstract or conclusion.

A proposed suite contains 30 in-scope question/history/state cases: five each for generic next steps, component-experiment entry, cross-module exit, SNOM waiting, instrument/specimen localization, and necessary clarification. Ten additional cases cover out-of-scope requests or unavailable capabilities. Run each case independently three times with fixed snapshots. Record model identity, prompt/knowledge versions, sampling parameters, candidates, regeneration, review, and client outcomes. These are proposed sample counts, not completed trials.

Before running the suite, evaluators familiar with the workflow should define acceptable actions or response categories for each in-scope case, allowing multiple valid next steps. Assess independently before resolving disagreements and report the actual number of evaluators and procedure. Judge both availability and correspondence to the learning goal: suggesting exit from an expanded model may be legal but unhelpful. A format repair remains part of the original user request.

Report first-candidate structural acceptance, final goal matching, refusal of normal questions, and service-error rates, with counts and explicit denominators: initial candidates for the first metric and in-scope requests for the others. Repair success uses repaired requests as its denominator and must state whether semantic and client checks also passed. Boundary cases are reported separately. Measure end-to-end time from user submission to a displayable result or terminal error, including repair and review, and distinguish success, failure, and timeout outcomes.

> **[FIGURE 5 PLACEHOLDER — OPTIONAL; REAL DATA REQUIRED]** Prefer a single-column plot of final outcomes for the six in-scope categories: goal-matched, legal but goal-mismatched, refusal, format/review error, and connection/timeout error. Use mutually exclusive categories and label request counts. If space permits, add an end-to-end timing distribution or individual points. Do not use mock results, hypothetical gains, or invented error bars. Do not duplicate the existing SUS/TLX table with an equivalent bar chart.
>
> **Proposed caption, after data collection:** Fig. 5. Final guidance outcomes for the live model across predefined state scenarios. Distributions use user requests as the unit and include outcomes after format repair and review; timing covers the complete question-answering pipeline.

## 7. Discussion and Limitations

The principal systems contribution of VRMicroscope is not to let a large language model "control the microscope," but to establish an explicit relationship among the current component, currently available controls, and observable consequences. The experimental environment provides manipulable objects and feedback, while the AI layer operates on top of those states to provide explanations, procedural instructions, and spatial guidance. State snapshots, the action catalog, and client-side revalidation reduce the risk of recommending nonexistent controls or stale steps, but they do not guarantee the scientific correctness of every natural-language explanation.

A second design priority is to distinguish instructional abstraction from physical fidelity. Focus visibility ranges and brightness mappings are deliberately authored teaching feedback. The NA, grating-diffraction, Fourier-spectrum, and s-SNOM modules are grounded in standard theoretical relationships, but they still use simplified parameters, synthetic imagery, or support-coverage visualizations. The system is therefore better characterized as an instructional simulation with explicit modeling boundaries than as a digital twin or quantitative microscopy simulator. A future scientific-fidelity evaluation should involve domain experts, content-validity review against real instrument behavior, and calibration of variables that can be compared with measured data.

The user study provides feasibility and usability evidence. Compared with the explicitly conditioned within-subject design used by Chheang et al. [1] and the AIxVR nursing study by Duan et al., which separates usability from learning-related outcomes and includes a comparison group [21], our single-group exploratory study is better suited to answering whether the system can be used and whether the workflow can be completed than whether AI improves learning. A subsequent evaluation should introduce AI-on/AI-off or alternative-tutoring conditions, pre/post microscopy concept tests, transfer tasks scored on real or expert-referenced procedures, and automatically logged measures such as AI interaction counts, speech error rates, and end-to-end latency. These additions would extend the evidence from "usable" toward reproducible claims about learning effectiveness and engineering performance.

## 8. Conclusion and Future Work

This paper presented VRMicroscope, an integrated learning environment that combines an interactive virtual microscopy laboratory, scientifically bounded instructional optics modules, and task-state-aware AI guidance. The system derives a set of available actions from the actual interface state, programmatically validates operational identifiers returned by the model, and rejects stale recommendations after state changes, while the VR layer retains concrete learning activities including specimen handling, focusing, illumination adjustment, objective switching, component exploration, numerical aperture, spatial frequency, and THz s-SNOM.

In an exploratory study with 19 participants, 18 completed the core task sequence and the whole-sample mean SUS score was 82.4/100, supporting workflow feasibility and high perceived usability. We do not interpret these results as causal evidence of learning gains from AI. Future work will focus on three directions: controlled experimental designs that isolate the incremental value of AI guidance; pre/post knowledge measures, expert ratings, and transfer tasks on real instruments; and systematic instrumentation of speech recognition, AI latency, and XR rendering performance.

## Research Ethics and Generative AI Disclosure

> **[AUTHOR COMPLETION REQUIRED BEFORE SUBMISSION]** Provide the actual ethics-review body and identifier, or explain why review was not performed. Describe participant consent, withdrawal, and the actual handling of questionnaires, audio, and cloud processing. This draft does not presume that approval or consent has already been documented.

Codex assisted with implementation and manuscript content generation, revision, and consistency checks; DeepSeek supplies runtime question answering. Human authors are responsible for verifying the implementation description, study records, analyses, and references. Identify any additional generative tools used for text, figures, or code and confirm the final disclosure against actual use.

## References

[1] V. Chheang et al., “Towards Anatomy Education with Generative AI-based Virtual Assistants in Immersive Virtual Reality Environments,” in *Proc. 2024 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 21–30, 2024. [doi:10.1109/AIxVR59861.2024.00011](https://doi.org/10.1109/AIxVR59861.2024.00011).

[2] A. Geris and G. Alce, “Real-Time Voice-Based LLM Integration for XR Tutoring: A Prototype Implementation,” in *Proc. 2026 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 285–289, 2026. [doi:10.1109/AIxVR67263.2026.00050](https://doi.org/10.1109/AIxVR67263.2026.00050).

[3] D. Wang et al., “SpatialTutor: Object-Aware Mixed Reality Training for Procedural Medical Skills Training with AI-Driven Support,” in *Proc. 2026 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 57–66, 2026. [doi:10.1109/AIxVR67263.2026.00016](https://doi.org/10.1109/AIxVR67263.2026.00016).

[4] J. Jonkman et al., “Tutorial: guidance for quantitative confocal microscopy,” *Nature Protocols*, vol. 15, pp. 1585–1611, 2020. [doi:10.1038/s41596-020-0313-9](https://doi.org/10.1038/s41596-020-0313-9).

[5] Carl ZEISS Microscopy, “Numerical Aperture and Light Cone Geometry.” [Online tutorial](https://www.zeiss.com/microscopy/en/resources/insights-hub/foundational-knowledge/numerical-aperture-and-light-cone-geometry.html). Accessed Sep. 13, 2026.

[6] Carl ZEISS Microscopy, “Spatial Frequency and Image Resolution.” [Online tutorial](https://www.zeiss.com/microscopy/en/resources/insights-hub/foundational-knowledge/spatial-frequency-and-image-resolution.html). Accessed Sep. 13, 2026.

[7] C. Eggeling, K. I. Willig, S. J. Sahl, and S. W. Hell, “Lens-based fluorescence nanoscopy,” *Quarterly Reviews of Biophysics*, vol. 48, no. 2, pp. 178–243, 2015. [doi:10.1017/S0033583514000146](https://doi.org/10.1017/S0033583514000146).

[8] E. Abbe, “Beiträge zur Theorie des Mikroskops und der mikroskopischen Wahrnehmung,” *Archiv für Mikroskopische Anatomie*, vol. 9, no. 1, pp. 413–468, 1873. [doi:10.1007/BF02956173](https://doi.org/10.1007/BF02956173).

[9] S. B. Mehta and R. Oldenbourg, “Image simulation for biological microscopy: microlith,” *Biomedical Optics Express*, vol. 5, no. 6, pp. 1822–1838, 2014. [doi:10.1364/BOE.5.001822](https://doi.org/10.1364/BOE.5.001822).

[10] F. J. Harris, “On the Use of Windows for Harmonic Analysis with the Discrete Fourier Transform,” *Proceedings of the IEEE*, vol. 66, no. 1, pp. 51–83, 1978. [doi:10.1109/PROC.1978.10837](https://doi.org/10.1109/PROC.1978.10837).

[11] J. W. Cooley and J. W. Tukey, “An Algorithm for the Machine Calculation of Complex Fourier Series,” *Mathematics of Computation*, vol. 19, no. 90, pp. 297–301, 1965. [doi:10.1090/S0025-5718-1965-0178586-1](https://doi.org/10.1090/S0025-5718-1965-0178586-1).

[12] M. G. L. Gustafsson, “Surpassing the lateral resolution limit by a factor of two using structured illumination microscopy,” *Journal of Microscopy*, vol. 198, no. 2, pp. 82–87, 2000. [doi:10.1046/j.1365-2818.2000.00710.x](https://doi.org/10.1046/j.1365-2818.2000.00710.x).

[13] R. Heintzmann and T. Huser, “Super-Resolution Structured Illumination Microscopy,” *Chemical Reviews*, vol. 117, no. 23, pp. 13890–13908, 2017. [doi:10.1021/acs.chemrev.7b00218](https://doi.org/10.1021/acs.chemrev.7b00218).

[14] P. U. Jepsen, D. G. Cooke, and M. Koch, “Terahertz spectroscopy and imaging – Modern techniques and applications,” *Laser & Photonics Reviews*, vol. 5, no. 1, pp. 124–166, 2011. [doi:10.1002/lpor.201000011](https://doi.org/10.1002/lpor.201000011).

[15] T. L. Cocker *et al*., “Nanoscale terahertz scanning probe microscopy,” *Nature Photonics*, vol. 15, pp. 558–569, 2021. [doi:10.1038/s41566-021-00835-6](https://doi.org/10.1038/s41566-021-00835-6).

[16] F. Keilmann and R. Hillenbrand, “Near-field microscopy by elastic light scattering from a tip,” *Philosophical Transactions of the Royal Society A: Mathematical, Physical and Engineering Sciences*, vol. 362, no. 1817, pp. 787–805, 2004. [doi:10.1098/rsta.2003.1347](https://doi.org/10.1098/rsta.2003.1347).

[17] R. Hillenbrand, B. Knoll, and F. Keilmann, “Pure optical contrast in scattering-type scanning near-field microscopy,” *Journal of Microscopy*, vol. 202, no. 1, pp. 77–83, 2001. [doi:10.1046/j.1365-2818.2001.00794.x](https://doi.org/10.1046/j.1365-2818.2001.00794.x).

[18] Groq, “Speech to Text.” [API documentation](https://console.groq.com/docs/speech-to-text). Accessed Sep. 13, 2026.

[19] J. Brooke, “SUS: A ‘Quick and Dirty’ Usability Scale,” in *Usability Evaluation in Industry*, 1996. [doi:10.1201/9781498710411-35](https://doi.org/10.1201/9781498710411-35).

[20] S. G. Hart and L. E. Staveland, “Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research,” *Advances in Psychology*, vol. 52, pp. 139–183, 1988. [Author text hosted by NASA](https://human-factors.arc.nasa.gov/publications/Hart_Staveland_ORIGINAL_1.pdf).

[21] Y. Duan, X. Xu, H. He, Y. Gu, S. Li, and J. Bueno Vesga, “Supplementing Patient Encounter Training for Preservice Nurses: Towards an AI×VR Approach,” in *Proc. 2026 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 98–107, 2026. [doi:10.1109/AIxVR67263.2026.00020](https://doi.org/10.1109/AIxVR67263.2026.00020).

[22] A. Bangor, P. T. Kortum, and J. T. Miller, “An Empirical Evaluation of the System Usability Scale,” *International Journal of Human–Computer Interaction*, vol. 24, no. 6, pp. 574–594, 2008. [doi:10.1080/10447310802205776](https://doi.org/10.1080/10447310802205776).
