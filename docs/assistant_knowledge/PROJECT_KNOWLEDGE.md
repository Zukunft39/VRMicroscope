# Project Knowledge and Explanation Boundaries

English edition: 2026-09-17. Grounded in project source and scene configuration; not a manufacturer certification or measured optics report.

Stable topic IDs: microscope_basics, numerical_aperture, spatial_frequency, confocal_background, snom, assistant_usage. Cite only topics actually supplied.

## Assistant capabilities (assistant_usage)

The orb animation, activation greetings, and idle reminders run locally without model calls. The chat provides English UI, topic examples, a basic English on-screen keyboard, cancellation, and new conversations. Submitted questions call DeepSeek through the configured backend. The model cannot independently inspect the computer, scene, or screen.

Current code supports state-based operating guidance, runtime location markers, and Groq speech transcription. Recording uses the default microphone for up to 30 seconds. Chinese or English speech is transcribed in its original language into an editable draft; the learner confirms before sending. Assistant replies are in English. There is no speech synthesis or automatic instrument execution. UI availability depends on current state and backend configuration.

Sources: Assets/m_Scripts/Assistant/AssistantChatPanel.cs, AssistantGuidance.cs, AssistantNavigation.cs, AssistantVoiceInput.cs; Tools/ai_tutor/assistant_chat.py and speech.py. Source implementation does not establish successful testing on every device.

## Microscope structure and basic operation (microscope_basics)

Configured components: Objective Lens, Upper Optical Assembly, Stage Position Control, Optical Breadboard, and Focus Adjustment. The first two link to spatial-frequency and NA experiments respectively. The other components have descriptions but no configured independent experiment entry.

Objectives focus and collect light. NA differs from magnification. Focusing represents relative axial positioning, not changing intrinsic objective focal length. Stage position control is not a confocal scanner. Internal components of Upper Optical Assembly are not individually identified; do not label them as a pinhole or dichroic mirror.

Four fluorescence specimen slides are placed in dishes in the laboratory:
- sample_red: bench beside the ultrasonic cleaner.
- sample_green: bench beside the centrifuge.
- sample_blue: left side of the workbench in front of the microscope.
- sample_yellow: right side of that workbench.

These descriptions identify their configured locations, not permanent runtime coordinates. The current navigation snapshot determines availability and relative position. Only after a marker is actually created may the client report its outline/light column. Pickup and placement are distinct from entering microscope observation.

Sources: ConfocalComponentBackground.md, scene component/specimen configuration, Microscope.cs, InteractableSamples.cs.

## Numerical aperture (numerical_aperture)

NA=n sin(theta), where theta is the half-angle between the optical axis and the collection-cone boundary. At fixed refractive index, increasing NA increases the half-angle. The default educational medium is air, n=1, with NA in 0.03-0.95. Only the current valid NA context establishes the current value.

Brightness, clarity, and depth tolerance are instructional mappings; approximate magnification interpolates seven reference settings. NA does not uniquely determine magnification. Display differences are not measured resolution or depth-of-field values.

Sources: NumericalApertureInteractionDesign.md and NumericalApertureExperimentController.cs.

## Spatial frequency, diffraction, and structured illumination (spatial_frequency)

High/Middle/Low correspond to 250/125/62.5 lines/mm. At fixed wavelength, smaller grating spacing D increases first-order diffraction angle. The diagram uses sin(psi)=lambda/D and back-focal-plane displacement f tan(psi).

The specimen spectrum derives from a selected texture, windowing, and a CPU 2D FFT, normally 128 by 128. Changing the grating carrier with the specimen fixed does not change its native spectrum; it changes sideband displacement and represented frequency support. Specimen-derived panels are empty without a selected specimen.

The middle panel shows a single orientation's separated sidebands, and the last combines support from three orientations. This is neither complete multiphase SIM reconstruction nor confocal pinhole filtering. White Light and Laser Excitation are instructional comparison modes.

Sources: SpatialFrequencyInteractionDesign.md, SpatialFrequencyExperimentController.cs, FourierOpticsCpuSimulator.cs.

## Confocal microscopy (confocal_background)

Explain focused excitation and collection, rejection of out-of-focus signals by a conjugate-image-plane pinhole, scanning image formation, and axial acquisition for a Z-stack as background concepts.

No confirmed pinhole adjustment, detector control, Z-stack acquisition, or full confocal scanning task is available in the current interaction catalog. Explain this limit when hands-on operation is requested. Structure and NA are related foundations, not equivalent replacement experiments. Refuse quantitative questions unsupported by the provided materials.

Source: ConfocalComponentBackground.md.

## THz s-SNOM (snom)

Learners select and install a probe, wait for installation, then start the system. The automatic tour covers:

| Stage | Concept | Observation |
|---|---|---|
| 1 | Broadband THz Pulse Generation | Pulse generation and illustrative time-domain waveform |
| 2 | Beam Steering and Focusing | THz steering and focusing path |
| 3 | AFM Distance Feedback | AFM readout and distance feedback |
| 4 | Tapping and Near-Field Coupling | Probe tapping and localized field |
| 5 | Weak Scattering over Background | Near-field signal versus background |
| 6 | Harmonic Demodulation | Higher harmonics for background suppression |
| 7 | Raster Scan | Point-by-point image formation |
| 8 | Correlated Results and Local Spectrum | Illustrative topography, amplitude, phase, and local spectrum |

The interaction sequence is snom_entry, snom_probe, snom_start, snom_controls. The tour advances automatically; infer the current stage only from current state or an unambiguous panel title. Previous/Next guidance must be allowed in the current snapshot.

The AFM readout laser differs from the THz excitation source; quadrant readout differs from THz detection. Local probe interactions differ from far-field objective imaging. 3x/2x/1x labels indicate illustrative relative detail, not objective magnification. Paths, probe animations, images, waveforms, and spectra are simplified synthetic teaching displays, not measured specimen material data. Do not invent physical radii, absolute resolution, or verified optical-path directions.

Sources: SNOMInteractionDesign.md, SNOMDemonstrationController.cs, SNOMDemonstrationGraphic.cs.
