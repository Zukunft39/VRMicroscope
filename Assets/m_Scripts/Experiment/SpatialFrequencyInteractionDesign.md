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
| Low | 62.5 lines/mm | 4 |

The diffraction angle is calculated from a 550 nm teaching wavelength. The order spacing uses an 18 mm objective focal length. These are geometric teaching values rather than a full physical ray-tracing simulation.

## CPU Fourier-optics pipeline

The authoritative result uses a 128 x 128 CPU simulation. It runs only when the experiment starts or when the player changes the frequency or illumination. No FFT is evaluated in `Update`, and no GPU FFT or compute shader is used.

1. The selected specimen or teaching pattern becomes a zero-phase object-plane amplitude field.
2. A two-dimensional FFT produces the objective back focal-plane spectrum.
3. A fixed circular objective pupil captures the frequency-dependent diffraction orders.
4. An inverse FFT reconstructs the pupil-filtered image.

The three cached `Texture2D` outputs are displayed as Object Plane, Fourier Spectrum, and Pupil-Filtered Reconstruction. The ordinary UI renderer only draws these cached textures.

## Illumination and input assumptions

- White Light visualizes wavelength-dependent order separation with representative red, green, and blue wavelengths.
- Laser Excitation uses one coherent spatial-frequency modulation. For teaching continuity, a selected specimen retains its RGB colour while the reconstructed image shows the coherent grating filtering.
- The current specimen is used as the object-plane input. If no specimen is selected, the experiment falls back to the teaching line grating.
- Player interaction is intentionally limited to High, Middle, and Low spatial-frequency states plus the two illumination modes.

## Scene setup

Run `Tools/Experiments/Setup Spatial Frequency Experiment` after Unity compiles the scripts. The setup creates the saved scene UI, wires transition references from the numerical-aperture experiment, and adds the Objective binding to the third selection-panel button.
