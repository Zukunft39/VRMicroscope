# Confocal Component Background

## Scope

The project is an educational microscope simulation with separate NA and
Fourier-domain experiments, not a calibrated confocal instrument simulation.
This review updates component copy without changing interaction bindings,
model geometry, materials, or image generation.

## Component audit

| Prefab group | Player-facing name | Interpretation |
| --- | --- | --- |
| ObjectLen | Objective Lens | Excitation focusing and fluorescence collection. NA and magnification are different properties. |
| AboveMirror | Upper Optical Assembly | An aggregate model linked to the NA lesson. Internal optics are not identified by the hierarchy. Do not label it a dichroic, condenser, or confocal pinhole without a model diagram. |
| Pole | Stage Position Control | X/Y specimen positioning, not the confocal beam scanner. |
| TT-D3030 | Optical Breadboard | A rigid aluminum honeycomb platform for mounting and aligning optical components, with vibration damping; not an active vibration-isolation table. [3] |
| AdjustmentKnob | Focus Adjustment | Relative axial positioning, not variable objective focal length. |

These are teaching mappings, not a manufacturer-verified parts inventory.
Explicit scene transform bindings remain unchanged.

The TT-D3030 model name matches CHUO's aluminum honeycomb breadboard. [3]
Its player-facing description is: "Provides a rigid, vibration-damping platform
for mounting and aligning optical components."

## Confocal background

A point-scanning fluorescence confocal microscope focuses excitation through
the objective and collects emission through the same objective. A detection
pinhole in a conjugate image plane rejects much of the out-of-focus signal.
Scanning assembles an image point by point; axial stepping produces a Z-stack.
The pinhole improves sectioning at the cost of detected signal when narrowed.
NA describes the accepted light cone, not magnification. [1]

Background component roles for future lessons:

| Component | Concise English explanation |
| --- | --- |
| Excitation Laser | Supplies light that excites the fluorescent specimen. |
| Dichroic Beamsplitter | Separates excitation and emission by wavelength. |
| Beam Scanner | Moves the focal spot across the specimen. |
| Emission Filter | Selects the detected wavelength band. |
| Confocal Pinhole | Rejects out-of-focus light before detection. |
| Detector | Converts collected light into an electrical signal. |

These roles do not imply that each component is separately modeled or
interactive in the current scene. They must not be assigned to arbitrary
meshes merely because those meshes resemble a mirror or lens.

## Keep the experiments distinct

The current spatial-frequency lesson explicitly illustrates grating diffraction
and structured-illumination frequency mixing. Its shifted spectra are not
evidence of confocal pinhole filtering. A Fourier-domain display alone does not
establish confocal optical sectioning. ZEISS identifies rejection of defocused
light by a pinhole as the basis of confocal optical sections. [2]

Retain the existing simulation, but explain these as complementary optical
principles rather than one physically complete acquisition pipeline. Do not
describe the ordinary specimen preview as a measured confocal optical section.

## Sources

1. Jonkman et al. (2020), *Tutorial: guidance for quantitative confocal microscopy*,
   Nature Protocols 15, 1585-1611. Figure 2 explains the optical components.
   https://doi.org/10.1038/s41596-020-0313-9
   Accessible article copy:
   https://bpb-us-e1.wpmucdn.com/sites.northwestern.edu/dist/a/560/files/2020/04/2020-Mar_NatureProtocols_Tutorial_guidance-for-quantitative-confocalmicroscopy.pdf
2. ZEISS, *Confocal Laser Scanning Microscopes*:
   https://www.zeiss.com/microscopy/us/products/light-microscopes/confocal-microscopes.html
3. CHUO Precision Industrial, *Aluminum honeycomb Bread Board*, TT-D3030:
   https://www.chuo.co.jp/english/contents/hp0145/list.php?CNo=145&ProCon=4196
