# Spatial Frequency Experiment Mapping

## Teaching relationship

The experiment visualizes the reciprocal relation between specimen line spacing and diffraction-order spacing in the objective back focal plane:

`S / f approximately equals lambda / D = sin(psi)`

- Higher spatial frequency means a smaller specimen line spacing `D`.
- A smaller `D` produces a larger diffraction angle `psi` and a larger order spacing `S`.
- Lower spatial frequency places diffraction orders closer together, allowing more orders to fit inside the objective back focal plane.
- At least the zero and first diffraction orders are required to resolve a line grating.

## Profiles

| Selection | Spatial frequency | Visible teaching orders |
| --- | ---: | ---: |
| High | 250 lines/mm | 1 |
| Middle | 125 lines/mm | 2 |
| Low | 50 lines/mm | 3 |

The diffraction angle is calculated from a 550 nm teaching wavelength. The order spacing uses an 18 mm objective focal length. These are geometric teaching values rather than a full physical ray-tracing simulation.

## Current specimen

When the player has selected a specimen, its texture is reused as a subtle overlay in the circular objective back focal plane. The calculated diffraction maxima remain procedural so the frequency relationship stays readable. If no specimen is available, the experiment uses the line-grating teaching view alone.

## Scene setup

Run `Tools/Experiments/Setup Spatial Frequency Experiment` after Unity compiles the scripts. The setup creates the saved scene UI, wires transition references from the numerical-aperture experiment, and adds the Objective binding to the third selection-panel button.
