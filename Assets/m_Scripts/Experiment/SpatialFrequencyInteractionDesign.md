# Spatial Frequency and Fourier-Domain Experiment

## Teaching focus

This experiment combines the original ZEISS-style Abbe diffraction tutorial
with the Fourier-domain model described in `ENG5059PReport_3045430W.docx`.
The ray path, white-light specimen, chromatic diffraction orders, and line
grating remain visible. A separate three-panel analysis band explains how
structured illumination moves otherwise inaccessible specimen frequencies into
the objective passband and how those components are separated and combined.

For a sinusoidal carrier, multiplication in the spatial domain becomes shifted
copies in the Fourier domain:

`G(k) = H(k) [S(k) + m/2 S(k-k0) + m/2 S(k+k0)]`

- `S(k)` is the specimen spectrum.
- `H(k)` is the objective optical transfer function (OTF).
- `k0` is the structured-light carrier vector.
- `m` is the modulation depth.
- Three phase steps conceptually separate the zero and `+/-1` components.
- Three carrier orientations extend support in two dimensions.

## Spatial-frequency states

| Selection | Grating frequency | Website-style result | Fourier result |
| --- | ---: | --- | --- |
| High | 250 lines/mm | Fine lines and widely separated diffraction orders | Largest carrier displacement |
| Middle | 125 lines/mm | Intermediate line and order spacing | Intermediate carrier displacement |
| Low | 62.5 lines/mm | Coarse lines and closely spaced diffraction orders | Smallest carrier displacement |

The active state uses a filled blue selection card, cyan outline, accent bar,
and bold white label. Inactive states use a light card and grey outline so the
current High/Middle/Low choice remains readable in desktop and XR views.

The Fourier panels display a contrast-enhanced specimen spectrum, the
phase-separated `+/-1` orders for one line-grating orientation inside the
objective OTF, and the recovered support from three illumination orientations.
Showing one orientation in the middle panel keeps the `+/-k0` displacement
readable; the final panel demonstrates the two-dimensional support gained by
combining three orientations. Brightness and sideband positions come from the
selected specimen's measured complex spectrum, while the dominant colour is
estimated from its illuminated pixels.
Explicit carrier dots and coloured support-circle outlines are intentionally
omitted.

The underlying `S(k)` belongs to the specimen and therefore does not physically
change when only the grating-frequency state changes. High/Middle/Low changes
the mixed and recovered spectra, while selecting a different specimen re-reads
the source texture and recomputes all three panels.

White Light preserves the specimen colour and shows a white zero order with
wavelength-separated blue, green, and red higher orders. Laser Excitation is
retained as a monochromatic comparison.

## Runtime constraints

- The FFT is `128 x 128` and runs on the CPU only when the experiment starts or
  a state changes.
- No FFT, texture generation, or reconstruction work runs in `Update`.
- No compute shader or GPU FFT is used. `Graphics.Blit` is used only to read the
  selected Unity texture into the CPU-sized input buffer.
- The visualization uses direct order separation and support fusion rather than
  the thesis's iterative FISTA reconstruction, keeping the result suitable for
  VR teaching while preserving the relevant Fourier relationship.
- If no specimen is selected, the original line grating and diffraction-order
  teaching graphics remain visible, while the three specimen-derived Fourier
  panels stay empty until a real specimen is selected.

## Scene setup

Run `Tools/Experiments/Setup Spatial Frequency Experiment` after Unity compiles
the scripts when the saved scene needs to be rebuilt. Existing scenes are also
updated at runtime so the restored reference graphics, compact Fourier strip,
illumination buttons, and High/Middle/Low controls use the combined layout.
