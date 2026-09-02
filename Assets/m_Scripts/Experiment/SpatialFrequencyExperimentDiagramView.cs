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
        RestoreReferenceViews();
        ApplyHybridLayout();
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

        if (fourierSimulator != null && sampleTexture != null)
        {
            fourierSimulator.Simulate(
                sampleTexture,
                normalizedFrequency,
                3);
            SetPlaneTexture(objectPlaneView, fourierSimulator.SpecimenSpectrumTexture);
            SetPlaneTexture(fourierPlaneView, fourierSimulator.MixedSpectrumTexture);
            SetPlaneTexture(reconstructedPlaneView, fourierSimulator.RecoveredSpectrumTexture);
        }
        else
        {
            SetPlaneTexture(objectPlaneView, null);
            SetPlaneTexture(fourierPlaneView, null);
            SetPlaneTexture(reconstructedPlaneView, null);
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
                ? "Specimen Fourier Spectrum S(k) | specimen-dependent"
                : "No specimen selected";
        }

        if (fourierPlaneLabel != null)
        {
            fourierPlaneLabel.text = sampleTexture != null
                ? $"Single-Orientation +/-1 Orders | {linesPerMillimeter:0.#} lines/mm"
                : "Select a specimen to calculate its Fourier spectrum";
        }

        if (reconstructedPlaneLabel != null)
        {
            reconstructedPlaneLabel.text = sampleTexture != null
                ? "Three-Orientation SIM Spectrum"
                : "Fourier analysis waiting for specimen";
        }
    }

    private void RestoreReferenceViews()
    {
        SetViewVisible(opticalPathGraphic, true);
        SetViewVisible(diffractionPatternGraphic, true);
        SetViewVisible(backFocalPlaneLabel, true);
        SetViewVisible(gratingLabel, true);
        SetViewVisible(sampleSourceLabel, true);

        if (backFocalSampleOverlay != null)
        {
            backFocalSampleOverlay.transform.parent.gameObject.SetActive(true);
            backFocalSampleOverlay.gameObject.SetActive(true);
        }

        if (lineGratingGraphic != null)
        {
            lineGratingGraphic.transform.parent.gameObject.SetActive(true);
            lineGratingGraphic.gameObject.SetActive(true);
        }
    }

    private void ApplyHybridLayout()
    {
        if (backFocalSampleOverlay != null)
        {
            SetRect((RectTransform)backFocalSampleOverlay.transform.parent,
                new Vector2(0.03f, 0.68f), new Vector2(0.35f, 0.95f));
        }

        SetComponentRect(diffractionPatternGraphic,
            new Vector2(0.40f, 0.80f), new Vector2(0.98f, 0.90f));
        if (lineGratingGraphic != null)
        {
            SetRect((RectTransform)lineGratingGraphic.transform.parent,
                new Vector2(0.40f, 0.65f), new Vector2(0.98f, 0.78f));
        }

        SetComponentRect(backFocalPlaneLabel,
            new Vector2(0.03f, 0.62f), new Vector2(0.35f, 0.68f));
        SetComponentRect(gratingLabel,
            new Vector2(0.40f, 0.59f), new Vector2(0.98f, 0.65f));
        SetComponentRect(sampleSourceLabel,
            new Vector2(0.36f, 0.54f), new Vector2(0.99f, 0.59f));

        SetPlaneRect(objectPlaneView, new Vector2(0.01f, 0.27f), new Vector2(0.32f, 0.51f));
        SetPlaneRect(fourierPlaneView, new Vector2(0.345f, 0.27f), new Vector2(0.655f, 0.51f));
        SetPlaneRect(reconstructedPlaneView, new Vector2(0.68f, 0.27f), new Vector2(0.99f, 0.51f));
        SetComponentRect(objectPlaneLabel,
            new Vector2(0.01f, 0.21f), new Vector2(0.32f, 0.27f));
        SetComponentRect(fourierPlaneLabel,
            new Vector2(0.335f, 0.21f), new Vector2(0.665f, 0.27f));
        SetComponentRect(reconstructedPlaneLabel,
            new Vector2(0.68f, 0.21f), new Vector2(0.99f, 0.27f));
    }

    private static void SetPlaneRect(RawImage view, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (view != null)
        {
            SetRect((RectTransform)view.transform.parent, anchorMin, anchorMax);
        }
    }

    private static void SetComponentRect(Component component, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (component != null)
        {
            SetRect((RectTransform)component.transform, anchorMin, anchorMax);
        }
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetViewVisible(Component component, bool visible)
    {
        if (component != null)
        {
            component.gameObject.SetActive(visible);
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
