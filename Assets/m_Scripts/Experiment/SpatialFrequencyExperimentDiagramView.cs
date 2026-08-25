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
    [SerializeField] private bool enableDebugLogs = true;

    private Material sampleFilterMaterial;

    public void ApplyProfile(
        float normalizedFrequency,
        float linesPerMillimeter,
        float diffractionAngleDegrees,
        int visibleOrderCount,
        Texture sampleTexture)
    {
        opticalPathGraphic?.SetFrequency(
            normalizedFrequency,
            diffractionAngleDegrees,
            visibleOrderCount);
        diffractionPatternGraphic?.SetPattern(
            normalizedFrequency,
            visibleOrderCount,
            sampleTexture != null ? 0.85f : 1f);
        lineGratingGraphic?.SetFrequency(normalizedFrequency);

        if (backFocalSampleOverlay != null)
        {
            backFocalSampleOverlay.texture = sampleTexture;
            backFocalSampleOverlay.enabled = sampleTexture != null;
            backFocalSampleOverlay.color = Color.white;
            float resolutionQuality = Mathf.InverseLerp(1f, 4f, visibleOrderCount);
            ApplySampleFilter(resolutionQuality, sampleTexture);
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
            sampleSourceLabel.text = sampleTexture != null
                ? "Reference diffraction orders for the selected spatial frequency"
                : "Reference diffraction orders: teaching grating fallback";
        }
    }

    private void ApplySampleFilter(float resolutionQuality, Texture sampleTexture)
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
        sampleFilterMaterial.SetFloat("_ResolutionQuality", appliedQuality);
        backFocalSampleOverlay.material = sampleFilterMaterial;
        backFocalSampleOverlay.SetMaterialDirty();

        // A UI Mask renders through a cached stencil material rather than the assigned
        // base material. Keep that final material synchronized when profiles change.
        Material renderingMaterial = backFocalSampleOverlay.materialForRendering;
        if (renderingMaterial != null && renderingMaterial.HasProperty("_ResolutionQuality"))
        {
            renderingMaterial.SetFloat("_ResolutionQuality", appliedQuality);
        }

        DebugSampleFilter(
            $"texture='{sampleTexture.name}', quality={appliedQuality:0.000}, " +
            $"baseShader='{sampleFilterMaterial.shader.name}', " +
            $"renderShader='{(renderingMaterial != null ? renderingMaterial.shader.name : "null")}', " +
            $"baseValue={ReadQuality(sampleFilterMaterial):0.000}, " +
            $"renderValue={ReadQuality(renderingMaterial):0.000}");
    }

    private static float ReadQuality(Material material)
    {
        return material != null && material.HasProperty("_ResolutionQuality")
            ? material.GetFloat("_ResolutionQuality")
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
