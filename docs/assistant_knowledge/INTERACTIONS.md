# Project Interaction Guide

English edition. Static catalog evidence does not grant runtime permissions. Use the current GUIDANCE_CONTEXT allowlist for operating instructions and NAVIGATION_CONTEXT for markers. Player actions are never executed by the assistant.

## navigation - Free exploration and viewpoint

**Location:** Main laboratory; fixed left/right routes from spawn are not established.

**Prerequisites:** Free movement must be permitted and not locked by a tutorial or experiment.

**Desktop Steps:** Move with W/A/S/D. Hold the right mouse button and move the mouse to look around. E/Q move up/down in the desktop controller; LeftShift accelerates.

**Xr Steps:** Input assets include left-thumbstick locomotion; the enabled headset locomotion setup requires device verification.

**Observation:** Player position and viewing direction change.

**Completion Evidence:** Arrival requires runtime position evidence, not conversation alone.

**Limitations:** CameraTryMove is enabled and Move is disabled in the audited main scene. Desktop debug vertical movement is not physical walking.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/CameraTryMove.cs, Assets/m_Scripts/Move.cs, Assets/m_Scripts/ProgressControl.cs

## teleport - Move toward a pointed target

**Location:** Accessible surfaces in the main laboratory.

**Prerequisites:** Movement must be unlocked and the target must pass ray, slope, and reachability checks.

**Desktop Steps:** The controller maps G to movement toward the pointed reachable ground.

**Xr Steps:** Roaming/AutoMove maps to gripButton, but Move is disabled in the main scene; this is not confirmed available XR guidance.

**Observation:** Movement toward the target may be constrained by collision and reachability.

**Completion Evidence:** Verify a change in runtime position.

**Limitations:** The XR route needs verification. Do not promise that every selected point is reachable or recommend an action absent from the live allowlist.

**Verification:** conditional; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/CameraTryMove.cs, Assets/m_Scripts/Move.cs

## sample_pick - Specimen selection and pickup

**Location:** Four dishes: red beside the ultrasonic cleaner, green beside the centrifuge, blue/yellow on the workbench in front of the microscope.

**Prerequisites:** Enter specimen range and register it as the current interactable. SNOM/assembly interactions take priority; removal takes priority if a specimen is already placed.

**Desktop Steps:** Approach an interactable specimen and left-click to pick it up.

**Xr Steps:** Approach an interactable specimen and press the right-hand trigger.

**Observation:** The runtime specimen and inventory texture update.

**Completion Evidence:** HasSampleOnHand, CurrentSampleTexture, and SampleChanged provide state evidence; use the current supplied snapshot.

**Exit Steps:** Another specimen can be selected. No generic discard key is confirmed.

**Limitations:** A ray hit alone does not establish registration. The old R-to-pickup instruction is incorrect.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Interact/InteractWithSamples.cs, Assets/m_Scripts/Interact/InteractableSamples.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## sample_place_observe - Place a specimen and enter observation

**Location:** The interactive microscope in the main laboratory, within its trigger range.

**Prerequisites:** Hold an ObserveObjects specimen and satisfy isNear and MicroUI.setTrue.

**Desktop Steps:** Press Z to place the specimen. When already placed, press Z to advance observation viewpoints until Observing.

**Xr Steps:** Press the left-hand trigger to place, then use the same input to advance observation viewpoints.

**Observation:** The specimen appears on the stage and the viewpoint enters observation.

**Completion Evidence:** Use HasPlacedSample and Interactor.CurrentState=Observing.

**Exit Steps:** In Observing, Z or Esc exits on desktop; the left-hand trigger exits in XR.

**Limitations:** Placement and pointer/viewpoint advancement are separate. A single press need not complete the entire sequence.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/Microscope.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## sample_remove - Remove the stage specimen

**Location:** Within interaction range of the microscope.

**Prerequisites:** Leave internal observation first; a specimen must be on the stage.

**Desktop Steps:** Left-click within microscope interaction range to remove the specimen.

**Xr Steps:** Press the right-hand trigger to remove the specimen.

**Observation:** The specimen moves from the microscope back to the player.

**Completion Evidence:** Check HasPlacedSample changes, not just the inventory icon.

**Limitations:** The same input also handles component/SNOM selection; use the appropriate current mode.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/Microscope.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## illumination - Microscope illumination and aperture display

**Location:** The interactive microscope and observation interface.

**Prerequisites:** Light toggling requires proximity and available external interaction.

**Desktop Steps:** Press B in external interaction mode to toggle the light. Hold X and use up/down arrows for illumination adjustment.

**Xr Steps:** Use the right-hand secondaryButton to toggle the light externally. Hold the left-hand primaryButton and move the right thumbstick up/down to adjust illumination.

**Observation:** The illustrative light/aperture and observation display change.

**Completion Evidence:** Use reported light state such as GetLight; a key press alone does not establish the resulting state.

**Limitations:** Q is desktop downward movement, not the old light shortcut. LightSwitch is not bound inside Observing.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/Microscope.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## objective_switch - Switch objectives

**Location:** The interactive microscope or internal observation view.

**Prerequisites:** Microscope interaction must be available; wait for turret rotation to finish.

**Desktop Steps:** Press R to switch objectives.

**Xr Steps:** Press the right-hand primaryButton to switch objectives.

**Observation:** The objective turret or displayed magnification changes.

**Completion Evidence:** A current objective/camera state report is needed to identify the actual setting.

**Limitations:** R means Replay in the SNOM tour. Magnification is not synonymous with NA.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/Microscope.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## focus - Coarse and fine focusing

**Location:** Microscope observation view.

**Prerequisites:** Enter Observing first.

**Desktop Steps:** Press Tab to switch coarse/fine mode. Hold X and use left/right arrows to focus.

**Xr Steps:** Press the right thumbstick to switch coarse/fine mode. Hold the left-hand primaryButton and move the right thumbstick left/right to focus.

**Observation:** Compare clarity and coarse/fine adjustment feedback.

**Completion Evidence:** Actual focus parameters and mode require a corresponding runtime report.

**Exit Steps:** Use Z/Esc on desktop or the left-hand trigger in XR to leave observation.

**Limitations:** Use the current input chain, not obsolete M/B/N shortcuts. Display feedback is not calibrated imaging.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/Microscope.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## assembly - Enter, expand, and restore the assembly

**Location:** The whole microscope assembly model in the main scene.

**Prerequisites:** No tutorial, experiment transition, or independent experiment may lock the mode.

**Desktop Steps:** Left-click the whole assembly for PreAssembly, then click again for SuperAssembly and wait for expansion.

**Xr Steps:** Point the right-hand ray at the assembly and press the trigger for PreAssembly; repeat for SuperAssembly.

**Observation:** The intact model becomes an exploded structure.

**Completion Evidence:** CurrentMode and ModelExploder.IsExploded/IsAnimating describe this state.

**Exit Steps:** Leave component selection first. Select outside the model to return SuperAssembly to PreAssembly, then outside again for Normal.

**Limitations:** Inputs are handled by stage. A single blank-area click does not exit every level.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/ExplodeModel/MicroscopeExploderModeController.cs, Assets/m_Scripts/ExplodeModel/ModelExploder.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## parts - Component inspection

**Location:** The expanded microscope components in SuperAssembly.

**Prerequisites:** Expansion must finish and no other experiment may lock selection.

**Desktop Steps:** Left-click a component and read its right-hand panel. Select outside the panel/component to leave the selection.

**Xr Steps:** Use the right-hand ray and trigger to select a component; select outside its UI/component to leave.

**Observation:** Configured names: Objective Lens, Upper Optical Assembly, Stage Position Control, Optical Breadboard, Focus Adjustment.

**Completion Evidence:** Use IsSelectionActive and current selectedPart/partDescription context.

**Exit Steps:** After leaving selection, follow the assembly return sequence.

**Limitations:** Internal optics of Upper Optical Assembly are not individually confirmed. Do not call it a dichroic mirror or pinhole. The last three listed components have no independent experiment button.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs

## na_experiment - Numerical-aperture experiment

**Location:** Upper Optical Assembly in the expanded model; internal node AboveMirror.

**Prerequisites:** Complete assembly expansion and select Upper Optical Assembly.

**Desktop Steps:** Select Start Numerical Aperture Experiment, wait, then drag the NA slider and compare angles, cone, and approximate magnification.

**Xr Steps:** Use the UI ray for the same button and slider. A fallback reads the x axis of Roaming/ChangeFocusOrChangeLIght, usually with the left primary modifier; verify on the actual device.

**Observation:** NA ranges from 0.03 to 0.95; theta, full angle, and cone change.

**Completion Evidence:** IsExperimentActive, IsTransitioning, CurrentNA, and CaptureTeachingState are implemented.

**Exit Steps:** Select Exit to return to the component; then use the component/assembly exit sequence.

**Limitations:** NA=n sin(theta). Magnification interpolates reference settings; brightness, clarity, and depth feedback are teaching mappings.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Experiment/NumericalApertureExperimentController.cs, Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs

## spatial_frequency - Spatial-frequency and diffraction experiment

**Location:** Objective Lens in the expanded model; internal node ObjectLen.

**Prerequisites:** Expand and select Objective Lens. Select a specimen first to see specimen-derived spectra.

**Desktop Steps:** Select Start Spatial Frequency Experiment and wait. High/Middle/Low are 250/125/62.5 lines/mm. Select White Light/Laser Excitation; hold specimen and illumination fixed for a carrier-only comparison.

**Xr Steps:** Use the right-hand UI ray for the same entry, frequency, and illumination controls.

**Observation:** Compare grating, diffraction spacing, specimen spectrum, sidebands, and multi-orientation support.

**Completion Evidence:** IsExperimentActive, CaptureTeachingState, and SampleChanged support state collection.

**Exit Steps:** Select Exit to return to the component view.

**Limitations:** Without a specimen, derived spectrum panels are empty. This is not confocal pinhole simulation or complete SIM image reconstruction.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Experiment/SpatialFrequencyExperimentController.cs, Assets/m_Scripts/Experiment/SpatialFrequencyExperimentDiagramView.cs, Assets/m_Scripts/Experiment/FourierOpticsCpuSimulator.cs, Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs

## snom_entry - Enter the SNOM demonstration

**Location:** Main-scene model TDs_edited_UnityVeryLowPoly; use runtime geometry for direction.

**Prerequisites:** Approach SNOM; its runtime controller must find the model and create the entry.

**Desktop Steps:** Click the SNOM target to show its entry, then select Begin Operation.

**Xr Steps:** Use the right-hand ray and trigger for the SNOM target, then select Begin Operation.

**Observation:** The probe-selection/installation interface appears.

**Completion Evidence:** Read the current workflow phase and stage from the controller snapshot.

**Exit Steps:** Select Exit, or Esc on desktop when available.

**Limitations:** RuntimeBootstrap creates this module dynamically. Absence of a directly attached scene script does not mean the feature is absent.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs

## snom_probe - Probe selection and installation

**Location:** SNOM ProbeSelection panel.

**Prerequisites:** Enter Begin Operation first.

**Desktop Steps:** Select Fine/Standard/Robust or press 1/2/3. Select Install Selected Probe or press E, then wait.

**Xr Steps:** Use the UI ray to select a probe card and Install Selected Probe.

**Observation:** The preview moves to its mount; Start System appears after installation.

**Completion Evidence:** ReadyToStart establishes completion of installation; the install click alone does not.

**Exit Steps:** Select Exit or use desktop Esc when available.

**Limitations:** 3x/2x/1x are relative-detail teaching labels, not objective magnifications. The meshes are not three calibrated physical probes.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs

## snom_start - Start SNOM and observe its principles

**Location:** SNOM ReadyToStart panel.

**Prerequisites:** Probe installation must finish and Start System must be available.

**Desktop Steps:** Select Start System or press Space.

**Xr Steps:** Select Start System with the UI ray.

**Observation:** Eight automatic stages cover THz generation, beam steering, AFM feedback, near-field coupling, scattering/background, demodulation, raster scan, and results/local spectrum.

**Completion Evidence:** Use the current stage and supplied playback state; do not invent scan progress.

**Exit Steps:** Select Exit or use desktop Esc when available.

**Limitations:** Installing and starting are player actions; the eight subsequent stages explain internal processes rather than eight additional instrument operations.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs, Assets/m_Scripts/Experiment/SNOMDemonstrationGraphic.cs

## snom_controls - SNOM tour and component controls

**Location:** SNOM PrincipleTour control bar.

**Prerequisites:** Start the system to enter the principle tour.

**Desktop Steps:** Previous/Next or left/right arrows change stage. Play/pause or Space controls playback; Replay or R repeats a stage. Components opens component explanations. Use Change Probe only when that button is displayed.

**Xr Steps:** Use the UI ray for currently visible Previous, Next, play/pause, Replay, and Components controls.

**Observation:** Animations, diagrams, and text change with the selected stage/component.

**Completion Evidence:** Use current stage, selection, and playback state.

**Exit Steps:** Select Exit or use desktop Esc when available.

**Limitations:** Separate Raw/2Omega/3Omega buttons are not confirmed despite earlier design proposals. Displayed results are synthetic.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs, Assets/m_Scripts/Experiment/SNOMDemonstrationGraphic.cs

## forced_tutorial - Mandatory interaction tutorial

**Location:** MandatoryTutorialTrigger and StandaloneTutorialUI panels.

**Prerequisites:** Meet the trigger, sequence, specimen, and state prerequisites.

**Desktop Steps:** Perform the input shown for the current step; not every step is a Space confirmation.

**Xr Steps:** Perform the requested controller action; combinations may require holding a modifier.

**Observation:** Matching input/events advance a step; some presentation steps advance automatically.

**Completion Evidence:** Use tutorial completion events and trigger state, not dialog claims.

**Exit Steps:** Complete the tutorial sequence; no universal skip control is confirmed.

**Limitations:** The tutorial may lock movement, assembly, or global input; suspend idle reminders while blocked.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Tutorial/ForceTutorial/MandatoryTutorialTrigger.cs, Assets/m_Scripts/Tutorial/ForceTutorial/StandaloneTutorialUI.cs, Assets/m_Scripts/Tutorial/ForceTutorial/ForceTutorialSequenceController.cs, Assets/m_Scripts/Tutorial/ForceTutorial/PlayerInputBlocker.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## optional_tutorial - Legacy optional tutorial entry

**Location:** Panel registered as Interactor.currentTutorial; the audited main-scene direct reference is null.

**Prerequisites:** Confirm a runtime Tutorial instance registers currentTutorial.

**Desktop Steps:** Code maps Y to open, arrows/WASD to navigation, and Space/Enter to confirm when the menu is active.

**Xr Steps:** Global/OpenTutorial maps to left secondaryButton; right thumbstick navigates and right primaryButton confirms.

**Observation:** A correctly registered instance displays tutorial lists or observation tutorials.

**Completion Evidence:** The direct main-scene attachment is not established; do not promise availability.

**Limitations:** This is not the assistant wake control. Old H documentation is not the current mapping. Missing registration risks null references.

**Verification:** conditional; runtime_verified=false

**Sources:** Assets/m_Scripts/Tutorial/Tutorial.cs, Assets/m_Scripts/Tutorial/TutorialButtonInput.cs, Assets/m_Scripts/Interactor/Interactor.cs, Assets/m_Scripts/Microscope/CameraTryMove.cs

## stage_translation - Candidate field-of-view translation

**Location:** ScreenMove needs an enabled instance on a target with a specimen child.

**Prerequisites:** Verify the actual component instance and enabled state.

**Desktop Steps:** Code reads Horizontal/Vertical (WASD/arrows), but current scene availability is not confirmed.

**Observation:** The specimen translates in its local XY plane.

**Completion Evidence:** Runtime attachment is unconfirmed.

**Limitations:** No direct main-scene reference was established; a prefab instance may exist. A script alone does not authorize operating instructions.

**Verification:** conditional; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/ScreenMove.cs

## filter_prototype - Filter and dichroic-mirror prototype

**Location:** PCDesktop scripts and light-path assets; no complete entry is confirmed.

**Prerequisites:** Actual UI, drag/drop, and slot binding need verification.

**Observation:** Filter, DichroicMirror, Slot, and light-line component code exists.

**Completion Evidence:** No complete learning workflow is confirmed.

**Limitations:** The corresponding PC_CanvasMgr.OpenApp branch is empty. Do not guide a learner to an unconfirmed application.

**Verification:** unavailable; runtime_verified=false

**Sources:** Assets/m_Scripts/PCDesktop/PC_CanvasMgr.cs, Assets/m_Scripts/PCDesktop/Slot.cs, Assets/m_Scripts/PCDesktop/Items/BaseItem.cs, Assets/m_Scripts/PCDesktop/Items/Filter.cs, Assets/m_Scripts/PCDesktop/Items/DichroicMirror.cs, Assets/m_Scripts/PCDesktop/LightLine/LightLineMgr.cs

## confocal_background - Confocal principles

**Location:** Knowledge explanation, related to microscope structure, NA, and spatial frequency; no full confocal interaction workflow.

**Observation:** Explain excitation, collection, rejection of defocus by a pinhole, and scanning as concepts.

**Completion Evidence:** There is no executable completion condition.

**Limitations:** Do not instruct pinhole adjustment, Z-stack acquisition, detector control, or real scanning parameters. NA/spectrum activities are not confocal optical sectioning.

**Verification:** knowledge_only; runtime_verified=false

**Sources:** Assets/m_Scripts/Microscope/Microscope.cs, Assets/m_Scripts/Experiment/FourierOpticsCpuSimulator.cs

## legacy_tutor - Removed fixed-question tutor

**Location:** Removed from NA and spatial-frequency experiments.

**Observation:** Old panels and fixed questions are unavailable; the aurora assistant remains.

**Completion Evidence:** Dedicated source, assets, editor menus, and the old /tutor route were removed.

**Limitations:** Historical entry retained only to prevent recommendations of the removed feature.

**Verification:** deprecated; runtime_verified=false

**Sources:** docs/assistant_knowledge/CHAT_IMPLEMENTATION.md

## aurora_assistant - Aurora orb assistant

**Location:** Upper-left of the main laboratory; runtime Local Aurora Assistant uses a camera-following world-space Canvas in XR.

**Prerequisites:** The main scene must run with LocalAssistantSettings.assistantEnabled=true. Verify display and input on the target device.

**Desktop Steps:** Press H or select the orb for a local greeting; H is ignored while editing input. Select Ask a question, enter a question or choose a topic, then Send. Voice Input records a draft for confirmation.

**Xr Steps:** Select the orb and controls with the existing XR UI ray; no additional experiment-controller shortcut is introduced.

**Observation:** Local greetings, idle reminders, English knowledge chat, confirmed speech drafts, current-state instructions, and location markers are implemented. Only submitted chat/transcription uses external services.

**Completion Evidence:** Runtime message, idle, guidance, and navigation state provide implementation evidence; device testing must be reported separately.

**Exit Steps:** Close the message, or select Snooze 5 min to pause idle reminders; manual activation remains available.

**Limitations:** Backend configuration is required for real model/voice use. Actions guide the learner and do not execute instrument controls. Historical stage-two limitations have been superseded by current state/navigation/voice code.

**Verification:** static_confirmed; runtime_verified=false

**Sources:** Assets/m_Scripts/Assistant/LocalAssistantController.cs, Assets/m_Scripts/Assistant/LocalAssistantSettings.cs, Assets/Resources/LocalAssistantSettings.asset, Assets/m_Scripts/Assistant/AssistantChatPanel.cs, Tools/ai_tutor/assistant_chat.py
