# Numerical Aperture Experiment Mapping

## Source behavior

The experiment follows the numerical-aperture and light-cone geometry teaching interaction. The slider is continuous across NA 0.03-0.95. The seven documented teaching profiles remain reference anchors, while values between anchors are interpolated for a smooth visual and magnification response.

| Slider step | NA | Half angle theta | Full aperture | Approx. magnification |
| --- | ---: | ---: | ---: | ---: |
| 1 | 0.03 | 1.7 deg | 3.4 deg | 1.25x |
| 2 | 0.085 | 4.9 deg | 9.8 deg | 2.5x |
| 3 | 0.16 | 9.2 deg | 18.4 deg | 5x |
| 4 | 0.25 | 14.5 deg | 29.0 deg | 10x |
| 5 | 0.50 | 30.0 deg | 60.0 deg | 20x |
| 6 | 0.75 | 48.6 deg | 97.2 deg | 40x |
| 7 | 0.95 | 71.8 deg | 143.6 deg | 63x |

## Numerical relationships

- Imaging medium: air, `n = 1.0`.
- Numerical aperture: `NA = n * sin(theta)`.
- Calculated half angle: `theta = asin(NA / n)`.
- Full angular aperture: `2 * theta`.
- Normalized value: `normalizedNA = (NA - 0.03) / (0.95 - 0.03)`.
- Visual response: increasing `normalizedNA` raises brightness and sharpness and lowers the displayed depth tolerance.

The displayed half-angle is calculated continuously from NA. Approximate magnification is linearly interpolated between the seven reference profiles. The same NA value drives the fallback 3D cone geometry.

## Diagram behavior

- The specimen remains fixed.
- The cone width at the objective entrance remains fixed.
- Moving the slider higher shortens the cone and moves the objective toward the specimen, producing a continuously larger half angle.
- The optical axis, theta arc, formula, value table, and approximate magnification update together.

## Unity modules

- `NumericalApertureExperimentController`: experiment state, continuous input, formulas, text, and visual-effect output.
- `NumericalApertureDiagramView`: objective/cone/specimen diagram geometry and theta arc.
- `NumericalApertureSceneSetup`: editor-only scene component creation, layout, sprite assignment, and controller reference wiring.
- `Assets/m_Images/Experiment/NumericalAperture`: separated transparent diagram sprites.
