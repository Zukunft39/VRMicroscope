using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpatialFrequencyExperimentDiagramView : MonoBehaviour
{
    [SerializeField] private SpatialFrequencyOpticalPathGraphic opticalPathGraphic;
    [SerializeField] private SpatialFrequencyDiffractionPatternGraphic diffractionPatternGraphic;
    [SerializeField] private SpatialFrequencyLineGratingGraphic lineGratingGraphic;
    [SerializeField] private RawImage backFocalSampleOverlay;
    [SerializeField] private Shader sampleFilterShader;
    [SerializeField] private TextMeshProUGUI backFocalPlaneLabel;
    [SerializeField] private TextMeshProUGUI gratingLabel;
    [SerializeField] private TextMeshProUGUI sampleSourceLabel;
    [Header("CPU Fourier Optics")]
    [SerializeField] private FourierOpticsCpuSimulator fourierSimulator;
    [SerializeField] private RawImage objectPlaneView;
    [SerializeField] private RawImage fourierPlaneView;
    [SerializeField] private RawImage reconstructedPlaneView;
    [SerializeField] private TextMeshProUGUI objectPlaneLabel;
    [SerializeField] private TextMeshProUGUI fourierPlaneLabel;
    [SerializeField] private TextMeshProUGUI reconstructedPlaneLabel;
    [SerializeField, Range(0f, 1f)] private float laserStripeStrength = 0.62f;
    [SerializeField] private bool enableDebugLogs;

    private Material sampleFilterMaterial;

    public void ApplyProfile(
        float normalizedFrequency,
        float linesPerMillimeter,
        float diffractionAngleDegrees,
        int visibleOrderCount,
        Texture sampleTexture,
        bool useLaserExcitation)
    {
        opticalPathGraphic?.SetFrequency(
            normalizedFrequency,
            diffractionAngleDegrees,
            visibleOrderCount);
        diffractionPatternGraphic?.SetPattern(
            normalizedFrequency,
            visibleOrderCount,
            sampleTexture != null ? 0.85f : 1f,
            useLaserExcitation);
        lineGratingGraphic?.SetFrequency(normalizedFrequency);

        if (fourierSimulator != null)
        {
            fourierSimulator.Simulate(
                sampleTexture,
                useLaserExcitation,
                normalizedFrequency,
                visibleOrderCount);
            SetPlaneTexture(objectPlaneView, fourierSimulator.ObjectPlaneTexture);
            SetPlaneTexture(fourierPlaneView, fourierSimulator.FourierPlaneTexture);
            SetPlaneTexture(reconstructedPlaneView, fourierSimulator.ReconstructedImageTexture);
        }

        if (backFocalSampleOverlay != null)
        {
            backFocalSampleOverlay.texture = sampleTexture;
            backFocalSampleOverlay.enabled = sampleTexture != null;
            backFocalSampleOverlay.color = Color.white;
            float resolutionQuality = Mathf.InverseLerp(1f, 4f, visibleOrderCount);
            ApplySampleFilter(
                resolutionQuality,
                normalizedFrequency,
                sampleTexture,
                useLaserExcitation);
        }

        if (backFocalPlaneLabel != null)
        {
            backFocalPlaneLabel.text = "Objective Back Focal Plane";
        }

        if (gratingLabel != null)
        {
            gratingLabel.text = $"Line grating with {linesPerMillimeter:0} lines/mm";
        }

        if (sampleSourceLabel != null)
        {
            if (useLaserExcitation)
            {
                sampleSourceLabel.text = sampleTexture != null
                    ? "Laser excitation: schematic coherent line-grating filtering; specimen colour retained"
                    : "Laser excitation: monochromatic teaching-grating diffraction orders";
            }
            else
            {
                sampleSourceLabel.text = sampleTexture != null
                    ? "White light: wavelength-dependent diffraction orders from the selected specimen"
                    : "White light: teaching-grating diffraction orders";
            }
        }

        if (objectPlaneLabel != null)
        {
            objectPlaneLabel.text = sampleTexture != null
                ? "Object Plane | Selected Specimen"
                : "Object Plane | Teaching Line Grating (No Specimen)";
        }

        if (fourierPlaneLabel != null)
        {
            fourierPlaneLabel.text = sampleTexture != null
                ? "Objective Back Focal Plane | Fourier Spectrum"
                : "Objective Back Focal Plane | Default Diffraction Orders";
        }

        if (reconstructedPlaneLabel != null)
        {
            reconstructedPlaneLabel.text = sampleTexture != null
                ? "Pupil-Filtered Reconstruction"
                : "Default Grating Reconstruction";
        }
    }

    private static void SetPlaneTexture(RawImage target, Texture texture)
    {
        if (target == null)
        {
            return;
        }

        target.texture = texture;
        target.color = Color.white;
        target.enabled = texture != null;
    }

    private void ApplySampleFilter(
        float resolutionQuality,
        float normalizedFrequency,
        Texture sampleTexture,
        bool useLaserExcitation)
    {
        if (sampleTexture == null)
        {
            backFocalSampleOverlay.material = null;
            DebugSampleFilter("No sample texture; the black teaching aperture is shown.");
            return;
        }

        if (sampleFilterMaterial == null)
        {
            Shader filterShader = sampleFilterShader != null
                ? sampleFilterShader
                : Shader.Find("UI/Spatial Frequency Sample Filter");
            if (filterShader == null)
            {
                Debug.LogError(
                    "[SpatialFrequencySample] Sample filter shader could not be resolved.",
                    this);
                return;
            }

            sampleFilterMaterial = new Material(filterShader)
            {
                name = "Spatial Frequency Sample Filter (Runtime)",
                hideFlags = HideFlags.DontSave
            };
        }

        sampleFilterMaterial.mainTexture = sampleTexture;
        float appliedQuality = Mathf.Clamp01(resolutionQuality);
        float laserMode = useLaserExcitation ? 1f : 0f;
        float stripeFrequency = Mathf.Lerp(9f, 34f, Mathf.Clamp01(normalizedFrequency));
        ApplySampleMaterialParameters(
            sampleFilterMaterial,
            appliedQuality,
            laserMode,
            stripeFrequency);
        backFocalSampleOverlay.material = sampleFilterMaterial;
        backFocalSampleOverlay.SetMaterialDirty();

        // A UI Mask renders through a cached stencil material rather than the assigned
        // base material. Keep that final material synchronized when profiles change.
        Material renderingMaterial = backFocalSampleOverlay.materialForRendering;
        ApplySampleMaterialParameters(
            renderingMaterial,
            appliedQuality,
            laserMode,
            stripeFrequency);

        DebugSampleFilter(
            $"texture='{sampleTexture.name}', quality={appliedQuality:0.000}, " +
            $"mode={(useLaserExcitation ? "laser" : "white-light")}, " +
            $"baseShader='{sampleFilterMaterial.shader.name}', " +
            $"renderShader='{(renderingMaterial != null ? renderingMaterial.shader.name : "null")}', " +
            $"baseValue={ReadQuality(sampleFilterMaterial):0.000}, " +
            $"renderValue={ReadQuality(renderingMaterial):0.000}, " +
            $"renderLaser={ReadFloat(renderingMaterial, "_LaserExcitation"):0.000}, " +
            $"stripeFrequency={ReadFloat(renderingMaterial, "_LaserStripeFrequency"):0.0}");
    }

    private void ApplySampleMaterialParameters(
        Material material,
        float quality,
        float laserMode,
        float stripeFrequency)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_ResolutionQuality"))
        {
            material.SetFloat("_ResolutionQuality", quality);
        }

        if (material.HasProperty("_LaserExcitation"))
        {
            material.SetFloat("_LaserExcitation", laserMode);
        }

        if (material.HasProperty("_LaserStripeFrequency"))
        {
            material.SetFloat("_LaserStripeFrequency", stripeFrequency);
        }

        if (material.HasProperty("_LaserStripeStrength"))
        {
            material.SetFloat("_LaserStripeStrength", laserStripeStrength);
        }
    }

    private static float ReadQuality(Material material)
    {
        return material != null && material.HasProperty("_ResolutionQuality")
            ? material.GetFloat("_ResolutionQuality")
            : -1f;
    }

    private static float ReadFloat(Material material, string propertyName)
    {
        return material != null && material.HasProperty(propertyName)
            ? material.GetFloat(propertyName)
            : -1f;
    }

    private void DebugSampleFilter(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SpatialFrequencySample] {message}", this);
        }
    }

    private void OnDestroy()
    {
        if (sampleFilterMaterial != null)
        {
            Destroy(sampleFilterMaterial);
        }
    }
}
