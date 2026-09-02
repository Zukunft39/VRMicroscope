# THz s-SNOM Guided Demonstration

## 1. Teaching objective

The SNOM demonstration is a guided component and principle tour rather than an
assembly or free-grab task. It explains how a THz time-domain source, focusing
optics, a tapping AFM probe, harmonic demodulation, and raster scanning produce
topography, near-field amplitude, phase, and local spectra.

The fixed scene model is the visual reference. The demonstration must never
move, rotate, or rescale the `TDs_edited_UnityVeryLowPoly` root. It also avoids
the microscope explanation behavior that temporarily moves a selected part in
front of the camera. Selection is represented with callouts, bounds highlights,
and local visual overlays while every physical part remains installed.

## 2. Non-negotiable scene constraint

The current main-scene prefab instance is intentionally positioned at:

- Local position: `(1.3010001, 0, 1.3569999)`
- Local rotation: approximately `(-90, 0, 0)`
- Local scale: preserve the existing imported value

The setup tool and runtime controller must cache this root pose but must not
write to it. Entering, replaying, stopping, disabling, or destroying the
demonstration must leave the root pose unchanged.

Allowed motion is limited to:

- An exaggerated, reversible probe tapping offset.
- Optional sub-millimetre scanner motion during the scan explanation.
- Runtime-only light paths, near-field overlays, scan cursors, graphs, labels,
  and UI.

All animated child transforms must be cached before playback and restored on
step exit, demonstration exit, `OnDisable`, and `OnDestroy`.

## 3. Relationship to the microscope explanation

Reuse these established project patterns:

- A world-space information panel with a component name and concise description.
- Right-hand XR ray selection and desktop pointer fallback.
- Explicit Previous, Play/Pause, Replay, Next, and Exit actions.
- `StandaloneTutorialUI` step events when a mandatory guided sequence is wanted.
- A separate controller that owns state and always performs cleanup.

Do not reuse these microscope-selection behaviors:

- Do not isolate and hide all other parts.
- Do not move or enlarge the selected physical part toward the camera.
- Do not enter PreAssembly or SuperAssembly mode.
- Do not change the active microscope camera preset.

The SNOM panel should be anchored beside the system or follow the player at a
comfortable XR distance. It must never cover the probe/sample interaction area.

## 4. Model component catalogue

Component references should be resolved once during setup and serialized. Name
lookup is only a fallback and must not run every frame.

| Teaching component | Model node | Role |
| --- | --- | --- |
| Complete system | `TDs_edited_UnityVeryLowPoly/TDs_UnityRoot` | Fixed system reference |
| THz optical assembly | `MX-Thz_step.empties/MX-Thz_step` | Main THz and AFM hardware |
| THz antenna A | `笼式结构_ step/mirror_笼式结构_step/光导天线` | Assign as emitter or receiver after checking the final optical direction |
| THz antenna B | `笼式结构_ step/mirror_笼式结构_step/光导天线.001` | The other TDS emitter/receiver |
| Fast mirror A | `TR75_M-JP-Solidworks/超快反射镜` | THz beam steering |
| Fast mirror B | `TR75_M-JP-Solidworks.001/超快反射镜.001` | THz beam steering/collection |
| Parabolic mirror assembly | `笼式结构_ step/40立方体抛物镜安装20220601.STEP` | Focuses or collects THz radiation |
| AFM head | `触针固定块` | Holds probe, AFM laser, detector, scanner, and preamplifier |
| AFM probe | `触针固定块/探针` | Creates the localized near field and samples the surface |
| Probe mount | `触针固定块/针尖座` | Mechanical probe support |
| Probe positioning block | `触针固定块/MX-SNOM_AFM-TZJ-02探针定位块` | Probe alignment support |
| AFM laser | `触针固定块/激光器` | Reads cantilever deflection; not the THz illumination source |
| Quadrant detector | `触针固定块/四象限` | AFM feedback detector; not the THz detector |
| Scan stages | `触针固定块/滑台` and `滑台.001` | Coarse/fine positioning and scan explanation |
| Head preamplifier | `触针固定块/Hea_preAmp_v2` | Conditions the detected electrical signal |
| Optional detector | `TDs_UnityRoot/Detector` | Detached optional receiver module; do not include in the default TDS path until hardware identity is confirmed |
| Optional connector | `TDs_UnityRoot/Connector` | Detached detector adaptor/connector; explanation-only by default |

The suffixes of the two photoconductive antennas must not be used as scientific
proof of which one is the emitter. The editor setup should expose explicit
`emitterAntenna` and `receiverAntenna` references so the final direction can be
confirmed visually.

## 5. Demonstration structure

The sequence has ten stages. Stages can auto-advance, but the user can pause and
move backward or forward at any time. Only stages 3 and 8 require interaction in
the recommended guided version.

### Stage 0 - System overview

**Title:** `THz Scattering-Type Scanning Near-Field Optical Microscope`

**Explanation:**

This system combines THz time-domain spectroscopy with a tapping AFM probe. It
does not capture the full image through an objective. It measures one local
point at a time and constructs images while scanning.

**Visuals:**

- Fade in a subtle outline around the complete system.
- Show four category callouts: THz source/detection, focusing optics, AFM head,
  and scan/result processing.
- Keep every model renderer visible.

**Interaction:** `Start Tour` or `Explore Components`.

### Stage 1 - THz generation and TDS pair

**Title:** `Broadband THz Pulse Generation`

**Explanation:**

A femtosecond optical pulse excites the emitting photoconductive antenna and
produces a broadband THz transient. The second antenna samples the returned
field. Measuring field versus delay gives `E(t)`; a Fourier transform gives
amplitude and phase versus frequency.

**Visuals:**

- Highlight the two `光导天线` nodes one at a time.
- Display an orange pulse travelling away from the confirmed emitter.
- Display a small time-domain waveform `E(t)` beside the source callout.
- Do not highlight the AFM laser as the THz source.

### Stage 2 - Beam steering and focusing

**Title:** `Steer and Focus the THz Field`

**Explanation:**

Mirrors guide the THz beam and the parabolic optic concentrates it near the
metallic tip. The far-field focus alone is not nanoscale; nanoscale confinement
is created by the tip.

**Visuals:**

- Animate a low-segment-count orange line through serialized optical anchors.
- Pulse callouts on the two fast mirrors and the parabolic mirror.
- End the incident path at the probe tip, not inside or behind the sample.

**Accuracy rule:** the path is hidden until its anchor order has been verified
in the Unity Scene view. A straight emitter-to-tip fallback may be labelled
`schematic path`, but must not be presented as exact hardware alignment.

### Stage 3 - Identify the AFM probe

**Title:** `Select the Near-Field Probe`

**Explanation:**

The metallic tip is the spatial probe. Its radius, rather than the free-space
THz wavelength, primarily sets the achievable lateral resolution.

**Visuals:**

- Add a bright cyan ring and leader line around `探针`.
- Add secondary, dimmer labels for `针尖座` and the positioning block.
- Show a magnified schematic inset; do not enlarge or move the real mesh.

**Mandatory interaction:** the user points to the probe callout and confirms.
The physical probe mesh is extremely small, so the selectable target is a
runtime proxy collider around its renderer bounds, not a collider attached to
the imported mesh.

### Stage 4 - AFM feedback

**Title:** `Maintain the Tip-Sample Distance`

**Explanation:**

The AFM laser reflects from the cantilever toward the quadrant detector. Motion
of the reflected spot provides feedback so the probe follows surface height
without crashing into the specimen.

**Visuals:**

- Draw a thin green path from `激光器` to a probe/cantilever anchor and then to
  `四象限`.
- Animate a small spot across a four-quadrant indicator.
- Show the feedback loop as `deflection -> error -> height correction`.

**Accuracy rule:** explicitly label this green path `AFM readout laser`; it is
not part of the THz illumination path.

### Stage 5 - Tapping and local near-field coupling

**Title:** `Confine the Field at the Tip`

**Explanation:**

The tip taps at frequency `Omega`. When it approaches the sample, antenna and
lightning-rod effects concentrate the field in the nanometre-scale gap. The tip
dipole couples to an image dipole in the sample, so local permittivity and
conductivity alter the scattered amplitude and phase.

**Visuals:**

- Animate only the cached probe animation anchor along scene-world up.
- Use an exaggerated visible amplitude, while the label states that the actual
  motion and gap are much smaller.
- Pulse an amber near-field hotspot only in the tip-sample gap.
- Draw a few short field arcs and a mirrored dipole below the sample surface.
- Show `z(t) = z0 + A cos(Omega t)`.

**Accuracy rule:** never draw the near field as a ray passing through the
sample. The hotspot must rapidly fade as the illustrated gap grows.

### Stage 6 - Scattering and background

**Title:** `Collect a Weak Near-Field Signal`

**Explanation:**

The detector receives both tip-scattered near-field information and a much
larger far-field background. The near-field contribution can be only about
`10^-3` to `10^-4` of the total scattered signal.

**Visuals:**

- Animate a cyan scattered path from the tip toward the confirmed receiver.
- Display a large grey `background` bar and a small cyan `near field` bar.
- Show `measured scattering = background + near field`.

### Stage 7 - Harmonic demodulation

**Title:** `Reject Background with Harmonic Detection`

**Explanation:**

The local field changes nonlinearly with tip-sample distance. Tapping therefore
places near-field information at harmonics of the tapping frequency. Reading
`2 Omega` or `3 Omega` suppresses weakly modulated far-field background.

**Visuals:**

- Present three compact traces: raw/`1 Omega`, `2 Omega`, and `3 Omega`.
- Fade the background component when switching to higher harmonics.
- Keep the higher-harmonic signal lower in absolute amplitude; do not imply
  that demodulation creates energy.

**Optional interaction:** buttons for `Raw`, `2 Omega`, and `3 Omega`.

### Stage 8 - Raster scanning

**Title:** `Build the Image Point by Point`

**Explanation:**

The system records AFM height and demodulated near-field response at each scan
position. A serpentine raster gradually forms registered topography, amplitude,
and phase maps.

**Visuals:**

- Create a runtime sample tile under the tip if no experiment specimen exists.
- Move a scan cursor over the tile in a serpentine path.
- Reveal the result textures line by line.
- The model root remains fixed. Prefer moving the runtime scan marker; scanner
  motion is optional and must restore exactly.

**Mandatory interaction:** the user presses `Start Scan`. A `Scan Speed` choice
may alter animation duration, not the simulated spatial resolution.

### Stage 9 - TDS spectrum and correlated results

**Title:** `Interpret Topography, Near Field, and Spectrum`

**Explanation:**

AFM topography describes surface height. Near-field amplitude and phase describe
local electromagnetic response. Selecting a pixel shows its time-domain pulse
and Fourier-domain spectrum, allowing regions with similar height but different
material properties to be distinguished.

**Visuals:**

- Show three registered panels: `AFM Topography`, `Near-Field Amplitude`, and
  `Near-Field Phase`.
- Selecting a map point updates `E(t)`, amplitude spectrum, and phase spectrum.
- Use prepared CPU textures or lightweight procedural data; no GPU FFT is
  required for this component-focused tour.

**Completion choices:** `Replay`, `Free Component Explore`, and `Exit`.

## 6. Free component exploration

Free exploration is available before or after the guided sequence. Selecting a
callout opens the same style of compact information panel used by the microscope
part explanation, but keeps the component in place.

Recommended catalogue entries:

| Label | Short description |
| --- | --- |
| Photoconductive Antenna | Converts an ultrafast optical pulse to a THz transient, or samples the returned transient in the receiver arm. |
| Fast Steering Mirror | Redirects the THz beam while preserving pulse timing and alignment. |
| Parabolic Mirror | Focuses or collects broadband THz radiation without chromatic focusing. |
| AFM Probe | Localizes the field and converts the tip-sample response into scattered radiation. |
| AFM Readout Laser | Measures cantilever deflection; it is separate from THz illumination. |
| Quadrant Detector | Converts AFM laser spot displacement into a feedback error signal. |
| Scan Stage | Controls relative tip-sample position for raster acquisition. |
| Preamplifier | Conditions the weak detected electrical signal before demodulation. |
| Optional Detector | Detached receiver module whose exact hardware role must be confirmed before joining the TDS path. |
| Connector | Mechanical/electrical adaptor associated with the detached detector module. |

## 7. Interaction and UI layout

Use one world-space panel with three stable regions:

1. Header: stage number, title, and progress rail.
2. Body: two-to-four sentence explanation plus the active formula or signal.
3. Footer: `Previous`, `Play/Pause`, `Replay Step`, `Next`, and `Exit`.

The component name callout should be visually separate from the main panel so
its leader line can terminate near the highlighted part. Use blue/cyan for
information and selection, amber for incident THz/near-field energy, green for
AFM laser feedback, and cyan for collected scattering. Avoid rainbow colouring
unless wavelength content is explicitly being taught.

XR controls:

- Right ray hover: reveal a component label.
- Right trigger: select a callout or press a UI control.
- Thumbstick left/right: previous/next when the pointer is not over another UI.
- Primary button: play/pause.
- Secondary button: exit confirmation.

Desktop fallback:

- Left click: select.
- Left/Right Arrow: previous/next.
- Space: play/pause.
- R: replay step.
- Escape: exit confirmation.

Do not require direct grabbing of scientific components. Accidental grabbing
would conflict with the fixed-system rule and provide little teaching value.

## 8. Runtime architecture

Recommended modules:

- `SNOMDemonstrationController`: stage state machine, transition cancellation,
  root-pose invariant, playback, and cleanup.
- `SNOMComponentCatalog`: serialized part references, labels, descriptions, and
  bounds/proxy setup.
- `SNOMDemonstrationView`: world-space UI, progress, callouts, graphs, and maps.
- `SNOMOpticalPathView`: incident THz, scattered THz, and AFM laser paths.
- `SNOMProbeAnimation`: reversible tapping and scan-cursor animation.
- `SNOMDemonstrationSceneSetup`: editor-only installer that finds the existing
  fixed root, creates a sibling `SNOM_DemonstrationSystem`, resolves references,
  and never writes the root transform.

Proposed scene hierarchy:

```text
SNOM_DemonstrationSystem
|-- SNOMDemonstrationController
|-- UIAnchor
|   `-- DemonstrationCanvas
|-- RuntimeVisuals
|   |-- ComponentHighlight
|   |-- IncidentTHzPath
|   |-- ScatteredTHzPath
|   |-- AFMLaserPath
|   |-- NearFieldHotspot
|   |-- ScanOverlay
|   `-- ResultDisplay
`-- Anchors
    |-- Emitter
    |-- Mirror01
    |-- ParabolicMirror
    |-- ProbeTip
    |-- Receiver
    `-- SamplePlane

TDs_edited_UnityVeryLowPoly  (existing fixed object; unchanged)
```

The demonstration system should be a sibling, not a replacement parent of the
existing SNOM model. Optical anchors may follow target components through
references but must not re-parent imported model parts.

## 9. Transition and cleanup rules

- Use one cancellable stage coroutine/tween sequence at a time.
- Stop the previous stage before starting another.
- Restore child animation poses before applying the next stage.
- Never call `Destroy` on existing model objects, interaction managers, or the
  global `Interactor`.
- Destroy only runtime-generated overlays owned by the demonstration.
- Null-check cached Unity objects during `OnDisable` and application shutdown.
- Do not start coroutines from objects that are being disabled or destroyed.
- Store no model lookup in `Update`; cache every renderer and transform.

These rules specifically prevent the destroyed-object cleanup failure that can
occur when an experiment tries to access `Interactor` during scene shutdown.

## 10. VR performance budget

- No GPU FFT is required for this tour.
- Use no more than three active `LineRenderer` paths at once.
- Use 16-32 segments for the near-field arcs and signal traces.
- Avoid dense particles; a small pooled set of travelling pulse markers is
  sufficient.
- Generate maps only when entering the result stage, not every frame.
- Use runtime proxy colliders only for the small set of teaching components.
- Do not duplicate all 377 imported renderers for highlighting.
- Prefer bounds rings, callout lines, and `MaterialPropertyBlock` overlays over
  cloned materials.

## 11. Acceptance criteria

1. The SNOM root position, rotation, scale, and parent are identical before,
   during, and after the demonstration.
2. Every stage can be entered directly, replayed, interrupted, and exited
   without leaving animation residue.
3. The AFM laser, THz incident path, and scattered path use distinct colours and
   are never described as the same signal.
4. The near-field visualization stays localized to the tip-sample gap.
5. `2 Omega` and `3 Omega` visibly suppress background without becoming brighter
   than the raw total signal.
6. Raster scanning builds maps point by point and does not imply camera-based
   full-field imaging.
7. Topography and near-field results remain registered but visually distinct.
8. The two photoconductive antennas are explicitly assigned as emitter and
   receiver after optical-path verification.
9. `Detector` and `Connector` remain optional until their hardware role is
   confirmed.
10. The flow works with XR ray input and desktop fallback at the project target
    frame rate.

## 12. Recommended implementation order

1. Install the controller and non-moving component callouts.
2. Verify antenna roles and create serialized optical anchors in Scene view.
3. Implement incident/scattered/AFM paths and probe tapping.
4. Add near-field coupling and harmonic-demodulation diagrams.
5. Add raster scanning and prepared result maps.
6. Connect optional `StandaloneTutorialUI` mandatory steps.
7. Profile in the target VR headset and tune visual complexity.

## 13. Current PC validation entry

The runtime implementation automatically detects
`TDs_edited_UnityVeryLowPoly` after a scene loads. If no persistent controller
exists, it creates `SNOM_DemonstrationSystem (Runtime)`, but keeps all tutorial
UI hidden. Point at the SNOM system and use the normal primary interaction
(PC left click or XR right trigger) to reveal a compact `Start SNOM Tour`
launcher. Only pressing that button opens the guided explanation panel. This
matches the microscope's device-gated interaction flow and prevents tutorial UI
from obscuring the laboratory before the player deliberately selects SNOM.

The invisible runtime entry proxy follows the existing model bounds for ray
selection only. It never reparents, moves, rotates, or rescales the SNOM model.
Exiting the tour hides all UI again, so another explicit system interaction is
required before restarting it.

For a persistent scene object, run:

`Tools/Experiments/Setup SNOM Demonstration`

The setup command creates `SNOM_DemonstrationSystem` as a sibling of the fixed
model, binds the existing root, verifies that its parent and local pose are
unchanged, and marks only the additive scene setup as dirty.

PC controls during the tour:

- Mouse: click UI controls or runtime component proxies.
- Left/Right Arrow: previous/next stage or component.
- Space: play/pause; on Stage 9 it starts the raster scan.
- R: replay the current stage.
- Escape: exit to the launcher.
