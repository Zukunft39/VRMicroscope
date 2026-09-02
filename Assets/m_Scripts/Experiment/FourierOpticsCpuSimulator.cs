using UnityEngine;

public sealed class FourierOpticsCpuSimulator : MonoBehaviour
{
    [SerializeField, Range(64, 256)] private int simulationResolution = 128;
    [SerializeField, Range(0.1f, 0.35f)] private float objectiveCutoff = 0.2f;
    [SerializeField, Range(0.5f, 0.95f)] private float carrierToCutoffRatio = 0.75f;
    [SerializeField, Range(0f, 1f)] private float modulationDepth = 0.9f;
    [SerializeField, Range(0f, 0.5f)] private float spectralDetailEnhancement = 0.14f;
    [SerializeField] private bool enableDebugLogs;

    private float[] real;
    private float[] imaginary;
    private float[] sourceMagnitude;
    private float[] logarithmicMagnitude;
    private float[] normalizedMagnitude;
    private float[] mixedMagnitude;
    private float[] radialMagnitudeSums;
    private int[] radialMagnitudeCounts;
    private Color32[] sourcePixels;
    private Color32[] specimenSpectrumPixels;
    private Color32[] mixedSpectrumPixels;
    private Color32[] recoveredSpectrumPixels;
    private Texture2D sourceReadback;
    private Texture2D specimenSpectrumTexture;
    private Texture2D mixedSpectrumTexture;
    private Texture2D recoveredSpectrumTexture;
    private int allocatedResolution;
    private int sourceSignature;
    private float sourceLogMaximum = 1f;
    private Color sourceTint = new Color(0.08f, 0.72f, 0.95f, 1f);

    public Texture2D SpecimenSpectrumTexture => specimenSpectrumTexture;
    public Texture2D MixedSpectrumTexture => mixedSpectrumTexture;
    public Texture2D RecoveredSpectrumTexture => recoveredSpectrumTexture;

    public void Simulate(Texture specimenTexture, float normalizedFrequency, int orientationCount)
    {
        int size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(simulationResolution, 64, 256));
        EnsureBuffers(size);
        ReadSourcePixels(specimenTexture, size);
        BuildObjectField(specimenTexture != null, size);
        Fft2D(real, imaginary, size);
        BuildMagnitude();

        int safeOrientationCount = orientationCount >= 3 ? 3 : orientationCount > 0 ? 1 : 0;
        float carrierPixels = CalculateCarrierPixels(normalizedFrequency, size);
        BuildSpecimenSpectrum(size);
        BuildMixedSpectrum(safeOrientationCount, carrierPixels, size);
        BuildRecoveredSpectrum(safeOrientationCount, carrierPixels, size);
        UploadTextures();

        if (enableDebugLogs)
        {
            string sourceName = specimenTexture != null ? specimenTexture.name : "Teaching grating";
            Debug.Log(
                $"[FourierOpticsCPU] source='{sourceName}', instance=" +
                $"{(specimenTexture != null ? specimenTexture.GetInstanceID() : 0)}, " +
                $"signature=0x{sourceSignature:X8}, frequency={normalizedFrequency:0.000}, " +
                $"carrier={carrierPixels:0.00}px",
                this);
        }
    }

    private void EnsureBuffers(int size)
    {
        if (allocatedResolution == size && specimenSpectrumTexture != null)
        {
            return;
        }

        ReleaseTextures();
        allocatedResolution = size;
        int pixelCount = size * size;
        real = new float[pixelCount];
        imaginary = new float[pixelCount];
        sourceMagnitude = new float[pixelCount];
        logarithmicMagnitude = new float[pixelCount];
        normalizedMagnitude = new float[pixelCount];
        mixedMagnitude = new float[pixelCount];
        radialMagnitudeSums = new float[size];
        radialMagnitudeCounts = new int[size];
        sourcePixels = new Color32[pixelCount];
        specimenSpectrumPixels = new Color32[pixelCount];
        mixedSpectrumPixels = new Color32[pixelCount];
        recoveredSpectrumPixels = new Color32[pixelCount];
        specimenSpectrumTexture = CreateOutputTexture(size, "Specimen Fourier Spectrum (CPU)");
        mixedSpectrumTexture = CreateOutputTexture(size, "SIM Mixed Spectrum (CPU)");
        recoveredSpectrumTexture = CreateOutputTexture(size, "SIM Recovered Support (CPU)");
    }

    private static Texture2D CreateOutputTexture(int size, string textureName)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false, false)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
    }

    private void ReadSourcePixels(Texture source, int size)
    {
        if (source == null)
        {
            sourceSignature = 0;
            return;
        }

        if (sourceReadback == null || sourceReadback.width != size || sourceReadback.height != size)
        {
            if (sourceReadback != null)
            {
                Destroy(sourceReadback);
            }

            sourceReadback = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "Fourier Source Readback",
                hideFlags = HideFlags.DontSave
            };
        }

        RenderTexture temporary = RenderTexture.GetTemporary(
            size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;
        sourceReadback.ReadPixels(new Rect(0f, 0f, size, size), 0, 0, false);
        sourceReadback.Apply(false, false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);

        sourceReadback.GetPixels32().CopyTo(sourcePixels, 0);
        sourceSignature = CalculateSourceSignature(sourcePixels);
    }

    private void BuildObjectField(bool hasSpecimen, int size)
    {
        sourceTint = hasSpecimen
            ? CalculateSourceTint()
            : new Color(0.08f, 0.72f, 0.95f, 1f);
        float inverseSize = 1f / size;
        for (int y = 0; y < size; y++)
        {
            float normalizedY = (y + 0.5f) * inverseSize;
            float windowY = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * normalizedY);
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                float value;
                if (hasSpecimen)
                {
                    float signal = CalculateSampleSignal(sourcePixels[index]);
                    float normalizedX = (x + 0.5f) * inverseSize;
                    float windowX = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * normalizedX);
                    value = signal * windowX * windowY;
                }
                else
                {
                    value = EvaluateTeachingSpecimen(x, y, size);
                }

                // Multiplication by (-1)^(x+y) centres the zero frequency.
                real[index] = ((x + y) & 1) == 0 ? value : -value;
                imaginary[index] = 0f;
            }
        }
    }

    private static float CalculateSampleSignal(Color color)
    {
        // Peak-channel energy keeps blue fluorescence from being underweighted by
        // photopic luminance while the luminance term preserves specimen detail.
        float peak = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
        return Mathf.Sqrt(Mathf.Clamp01(0.72f * peak + 0.28f * luminance));
    }

    private static int CalculateSourceSignature(Color32[] pixels)
    {
        unchecked
        {
            const int fnvPrime = 16777619;
            int hash = (int)2166136261;
            int stride = Mathf.Max(1, pixels.Length / 1024);
            for (int index = 0; index < pixels.Length; index += stride)
            {
                Color32 pixel = pixels[index];
                hash = (hash ^ pixel.r) * fnvPrime;
                hash = (hash ^ pixel.g) * fnvPrime;
                hash = (hash ^ pixel.b) * fnvPrime;
            }

            return hash;
        }
    }

    private Color CalculateSourceTint()
    {
        Vector3 weightedColor = Vector3.zero;
        float totalWeight = 0f;
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            Color color = sourcePixels[i];
            float brightness = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.035f, 0.55f, brightness));
            if (weight <= 0f)
            {
                continue;
            }

            weightedColor += new Vector3(color.r, color.g, color.b) * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0.001f)
        {
            return Color.white;
        }

        Vector3 average = weightedColor / totalWeight;
        float maximum = Mathf.Max(average.x, Mathf.Max(average.y, average.z));
        if (maximum > 0.001f)
        {
            average /= maximum;
        }

        return new Color(average.x, average.y, average.z, 1f);
    }

    private static float EvaluateTeachingSpecimen(int x, int y, int size)
    {
        float px = (x + 0.5f) / size * 2f - 1f;
        float py = (y + 0.5f) / size * 2f - 1f;
        float diagonal = Mathf.Exp(-Mathf.Pow((py - 0.45f * px) * 16f, 2f));
        float opposite = Mathf.Exp(-Mathf.Pow((py + 0.7f * px - 0.12f) * 20f, 2f));
        float ring = Mathf.Exp(-Mathf.Pow((Mathf.Sqrt(px * px + py * py) - 0.38f) * 24f, 2f));
        return Mathf.Clamp01(diagonal + 0.75f * opposite + 0.55f * ring) - 0.2f;
    }

    private void BuildMagnitude()
    {
        System.Array.Clear(radialMagnitudeSums, 0, radialMagnitudeSums.Length);
        System.Array.Clear(radialMagnitudeCounts, 0, radialMagnitudeCounts.Length);
        float maximum = 0f;
        int size = allocatedResolution;
        float centre = (size - 1) * 0.5f;
        for (int i = 0; i < sourceMagnitude.Length; i++)
        {
            float magnitude = Mathf.Sqrt(real[i] * real[i] + imaginary[i] * imaginary[i]);
            float logarithmic = Mathf.Log(1f + magnitude);
            sourceMagnitude[i] = magnitude;
            logarithmicMagnitude[i] = logarithmic;
            maximum = Mathf.Max(maximum, logarithmic);

            int x = i % size;
            int y = i / size;
            int radialBin = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Sqrt(
                    (x - centre) * (x - centre) + (y - centre) * (y - centre))),
                0,
                radialMagnitudeSums.Length - 1);
            radialMagnitudeSums[radialBin] += logarithmic;
            radialMagnitudeCounts[radialBin]++;
        }

        sourceLogMaximum = maximum > 0f ? maximum : 1f;
        float inverseMaximum = 1f / sourceLogMaximum;
        for (int i = 0; i < normalizedMagnitude.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            int radialBin = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Sqrt(
                    (x - centre) * (x - centre) + (y - centre) * (y - centre))),
                0,
                radialMagnitudeSums.Length - 1);
            float radialMean = radialMagnitudeCounts[radialBin] > 0
                ? radialMagnitudeSums[radialBin] / radialMagnitudeCounts[radialBin]
                : 0f;
            float baseValue = logarithmicMagnitude[i] * inverseMaximum;
            float flattened = radialMean > 0.0001f
                ? Mathf.Clamp01(logarithmicMagnitude[i] / (radialMean * 2.15f))
                : 0f;
            float enhanced = Mathf.Lerp(baseValue, Mathf.Max(baseValue * 0.72f, flattened),
                spectralDetailEnhancement);
            normalizedMagnitude[i] = Mathf.Pow(Mathf.Clamp01(enhanced), 0.68f);
        }
    }

    private void BuildSpecimenSpectrum(int size)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                Color color = SpectrumColor(normalizedMagnitude[index]);
                AddFrequencyAxes(ref color, x, y, size);
                specimenSpectrumPixels[index] = color;
            }
        }
    }

    private float CalculateCarrierPixels(float normalizedFrequency, int size)
    {
        float frequencyScale = Mathf.Lerp(0.22f, 1f, Mathf.Clamp01(normalizedFrequency));
        return objectiveCutoff * carrierToCutoffRatio * frequencyScale * size;
    }

    private void BuildMixedSpectrum(int orientationCount, float carrierPixels, int size)
    {
        // A single grating orientation creates one pair of shifted specimen
        // spectra. Keeping this panel to one orientation makes k0 displacement
        // visible; the recovered panel below combines all three orientations.
        float maximum = 0f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                float pupil = EvaluatePupil(x, y, 0f, 0f, size);
                float mixedReal = 0f;
                float mixedImaginary = 0f;
                if (orientationCount > 0)
                {
                    float positiveReal = SampleComplex(real, x - carrierPixels, y, size);
                    float positiveImaginary = SampleComplex(imaginary, x - carrierPixels, y, size);
                    float negativeReal = SampleComplex(real, x + carrierPixels, y, size);
                    float negativeImaginary = SampleComplex(imaginary, x + carrierPixels, y, size);
                    float sidebandWeight = modulationDepth * 0.5f;
                    mixedReal += sidebandWeight * (positiveReal + negativeReal);
                    mixedImaginary += sidebandWeight *
                        (positiveImaginary + negativeImaginary);
                }

                float magnitude = Mathf.Sqrt(
                    mixedReal * mixedReal + mixedImaginary * mixedImaginary) * pupil;
                mixedMagnitude[index] = magnitude;
                maximum = Mathf.Max(maximum, Mathf.Log(1f + magnitude));
            }
        }

        // Keep most of the specimen-spectrum scale so changing k0 cannot be
        // hidden by independent per-state normalization.
        float displayMaximum = Mathf.Max(sourceLogMaximum * 0.82f, maximum * 0.72f);
        float inverseMaximum = displayMaximum > 0f ? 1f / displayMaximum : 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                float value = Mathf.Clamp01(Mathf.Log(1f + mixedMagnitude[index]) * inverseMaximum);
                Color color = SpectrumColor(Mathf.Pow(value, 1.35f));
                AddFrequencyAxes(ref color, x, y, size);
                mixedSpectrumPixels[index] = color;
            }
        }
    }

    private void BuildRecoveredSpectrum(int orientationCount, float carrierPixels, int size)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                float centralCoverage = EvaluatePupil(x, y, 0f, 0f, size);
                float shiftedCoverage = 0f;

                for (int orientation = 0; orientation < orientationCount; orientation++)
                {
                    float angle = orientation * Mathf.PI / 3f;
                    float offsetX = Mathf.Cos(angle) * carrierPixels;
                    float offsetY = Mathf.Sin(angle) * carrierPixels;
                    float positive = EvaluatePupil(x, y, offsetX, offsetY, size);
                    float negative = EvaluatePupil(x, y, -offsetX, -offsetY, size);
                    float pairCoverage = positive + negative;
                    shiftedCoverage += pairCoverage;
                }

                float coverage = Mathf.Clamp01(centralCoverage + shiftedCoverage);
                float spectralValue = normalizedMagnitude[index] * coverage;
                Color color = SpectrumColor(Mathf.Pow(Mathf.Clamp01(spectralValue), 0.68f));
                AddFrequencyAxes(ref color, x, y, size);
                recoveredSpectrumPixels[index] = ClampColor(color);
            }
        }
    }

    private static float SampleComplex(float[] values, float x, float y, int size)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        if (x0 < 0 || y0 < 0 || x1 >= size || y1 >= size)
        {
            return 0f;
        }

        float tx = x - x0;
        float ty = y - y0;
        float bottom = Mathf.Lerp(values[y0 * size + x0], values[y0 * size + x1], tx);
        float top = Mathf.Lerp(values[y1 * size + x0], values[y1 * size + x1], tx);
        return Mathf.Lerp(bottom, top, ty);
    }

    private float EvaluatePupil(float x, float y, float offsetX, float offsetY, int size)
    {
        float centre = (size - 1) * 0.5f;
        float normalizedX = (x - centre - offsetX) / size;
        float normalizedY = (y - centre - offsetY) / size;
        float radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
        float edge = Mathf.Clamp01((objectiveCutoff - radius) * size * 0.7f);
        return edge * edge * (3f - 2f * edge);
    }

    private static void AddFrequencyAxes(ref Color color, int x, int y, int size)
    {
        int centre = size / 2;
        if ((x == centre || y == centre) && Mathf.Abs(x - centre) + Mathf.Abs(y - centre) > 4)
        {
            color = Color.Lerp(color, new Color(0.12f, 0.18f, 0.28f), 0.22f);
        }
    }

    private Color SpectrumColor(float value)
    {
        value = Mathf.Clamp01(value);
        Color low = Color.Lerp(new Color(0.008f, 0.012f, 0.025f, 1f), sourceTint, 0.055f);
        Color middle = sourceTint * 0.78f;
        middle.a = 1f;
        Color high = Color.Lerp(sourceTint, Color.white, 0.28f);
        return value < 0.55f
            ? Color.Lerp(low, middle, value / 0.55f)
            : Color.Lerp(middle, high, (value - 0.55f) / 0.45f);
    }

    private static Color ClampColor(Color color)
    {
        return new Color(
            Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), 1f);
    }

    private void UploadTextures()
    {
        specimenSpectrumTexture.SetPixels32(specimenSpectrumPixels);
        specimenSpectrumTexture.Apply(false, false);
        mixedSpectrumTexture.SetPixels32(mixedSpectrumPixels);
        mixedSpectrumTexture.Apply(false, false);
        recoveredSpectrumTexture.SetPixels32(recoveredSpectrumPixels);
        recoveredSpectrumTexture.Apply(false, false);
    }

    private static void Fft2D(float[] realValues, float[] imaginaryValues, int size)
    {
        for (int row = 0; row < size; row++)
        {
            Fft1D(realValues, imaginaryValues, row * size, 1, size);
        }

        for (int column = 0; column < size; column++)
        {
            Fft1D(realValues, imaginaryValues, column, size, size);
        }
    }

    private static void Fft1D(float[] realValues, float[] imaginaryValues, int offset, int stride, int length)
    {
        int swapIndex = 0;
        for (int index = 1; index < length; index++)
        {
            int bit = length >> 1;
            while ((swapIndex & bit) != 0)
            {
                swapIndex ^= bit;
                bit >>= 1;
            }

            swapIndex ^= bit;
            if (index >= swapIndex)
            {
                continue;
            }

            int first = offset + index * stride;
            int second = offset + swapIndex * stride;
            float realSwap = realValues[first];
            float imaginarySwap = imaginaryValues[first];
            realValues[first] = realValues[second];
            imaginaryValues[first] = imaginaryValues[second];
            realValues[second] = realSwap;
            imaginaryValues[second] = imaginarySwap;
        }

        for (int blockLength = 2; blockLength <= length; blockLength <<= 1)
        {
            float angle = -2f * Mathf.PI / blockLength;
            float phaseStepReal = Mathf.Cos(angle);
            float phaseStepImaginary = Mathf.Sin(angle);
            int halfLength = blockLength >> 1;

            for (int blockStart = 0; blockStart < length; blockStart += blockLength)
            {
                float phaseReal = 1f;
                float phaseImaginary = 0f;
                for (int pair = 0; pair < halfLength; pair++)
                {
                    int evenIndex = offset + (blockStart + pair) * stride;
                    int oddIndex = offset + (blockStart + pair + halfLength) * stride;
                    float oddReal = realValues[oddIndex] * phaseReal -
                        imaginaryValues[oddIndex] * phaseImaginary;
                    float oddImaginary = realValues[oddIndex] * phaseImaginary +
                        imaginaryValues[oddIndex] * phaseReal;
                    float evenReal = realValues[evenIndex];
                    float evenImaginary = imaginaryValues[evenIndex];

                    realValues[evenIndex] = evenReal + oddReal;
                    imaginaryValues[evenIndex] = evenImaginary + oddImaginary;
                    realValues[oddIndex] = evenReal - oddReal;
                    imaginaryValues[oddIndex] = evenImaginary - oddImaginary;

                    float nextPhaseReal = phaseReal * phaseStepReal -
                        phaseImaginary * phaseStepImaginary;
                    phaseImaginary = phaseReal * phaseStepImaginary +
                        phaseImaginary * phaseStepReal;
                    phaseReal = nextPhaseReal;
                }
            }
        }
    }

    private void OnDestroy()
    {
        ReleaseTextures();
        if (sourceReadback != null)
        {
            Destroy(sourceReadback);
        }
    }

    private void ReleaseTextures()
    {
        if (specimenSpectrumTexture != null)
        {
            Destroy(specimenSpectrumTexture);
        }

        if (mixedSpectrumTexture != null)
        {
            Destroy(mixedSpectrumTexture);
        }

        if (recoveredSpectrumTexture != null)
        {
            Destroy(recoveredSpectrumTexture);
        }

        specimenSpectrumTexture = null;
        mixedSpectrumTexture = null;
        recoveredSpectrumTexture = null;
    }
}
