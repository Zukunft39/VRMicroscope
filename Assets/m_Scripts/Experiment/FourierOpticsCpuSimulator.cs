using UnityEngine;

public sealed class FourierOpticsCpuSimulator : MonoBehaviour
{
    private const int ChannelCount = 3;

    [SerializeField, Range(64, 256)] private int simulationResolution = 128;

    private readonly float[][] real = new float[ChannelCount][];
    private readonly float[][] imaginary = new float[ChannelCount][];
    private Color32[] sourcePixels;
    private Color32[] objectPixels;
    private Color32[] spectrumPixels;
    private Color32[] reconstructionPixels;
    private Texture cachedSourceTexture;
    private Texture2D sourceReadback;
    private Texture2D objectPlaneTexture;
    private Texture2D fourierPlaneTexture;
    private Texture2D reconstructedImageTexture;
    private int allocatedResolution;

    public Texture2D ObjectPlaneTexture => objectPlaneTexture;
    public Texture2D FourierPlaneTexture => fourierPlaneTexture;
    public Texture2D ReconstructedImageTexture => reconstructedImageTexture;
    public void Simulate(
        Texture specimenTexture,
        bool useLaserExcitation,
        float normalizedFrequency,
        int visibleOrderCount)
    {
        int size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(simulationResolution, 64, 256));
        EnsureBuffers(size);
        ReadSourcePixels(specimenTexture, size);
        BuildObjectField(
            specimenTexture != null,
            useLaserExcitation,
            Mathf.Clamp01(normalizedFrequency),
            size);

        for (int channel = 0; channel < ChannelCount; channel++)
        {
            Fft2D(real[channel], imaginary[channel], size, inverse: false);
        }

        ApplyObjectivePupil(
            specimenTexture != null,
            Mathf.Clamp01(normalizedFrequency),
            Mathf.Max(1, visibleOrderCount),
            size);
        BuildFourierPlane(useLaserExcitation, specimenTexture != null, size);

        for (int channel = 0; channel < ChannelCount; channel++)
        {
            Fft2D(real[channel], imaginary[channel], size, inverse: true);
        }

        BuildReconstruction(size);
        UploadTextures();
    }

    private void EnsureBuffers(int size)
    {
        if (allocatedResolution == size && objectPlaneTexture != null)
        {
            return;
        }

        ReleaseTextures();
        allocatedResolution = size;
        int pixelCount = size * size;
        sourcePixels = new Color32[pixelCount];
        objectPixels = new Color32[pixelCount];
        spectrumPixels = new Color32[pixelCount];
        reconstructionPixels = new Color32[pixelCount];
        for (int channel = 0; channel < ChannelCount; channel++)
        {
            real[channel] = new float[pixelCount];
            imaginary[channel] = new float[pixelCount];
        }

        objectPlaneTexture = CreateOutputTexture(size, "Fourier Object Plane (CPU)");
        fourierPlaneTexture = CreateOutputTexture(size, "Fourier Plane (CPU)");
        reconstructedImageTexture = CreateOutputTexture(size, "Fourier Reconstruction (CPU)");
        cachedSourceTexture = null;
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
            cachedSourceTexture = null;
            return;
        }

        if (cachedSourceTexture == source && sourceReadback != null &&
            sourceReadback.width == size && sourceReadback.height == size)
        {
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
            size,
            size,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;
        sourceReadback.ReadPixels(new Rect(0f, 0f, size, size), 0, 0, false);
        sourceReadback.Apply(false, false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);

        sourceReadback.GetPixels32().CopyTo(sourcePixels, 0);
        cachedSourceTexture = source;
    }

    private void BuildObjectField(
        bool hasSpecimen,
        bool useLaserExcitation,
        float normalizedFrequency,
        int size)
    {
        float cycles = Mathf.Lerp(6f, 24f, normalizedFrequency);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                Color source = sourcePixels[index];
                float grating = EvaluateLineGrating(x, size, cycles);
                float pattern = hasSpecimen
                    ? useLaserExcitation ? 0.12f + 0.88f * grating : 1f
                    : grating;
                Color objectColor = hasSpecimen
                    ? new Color(source.r * pattern, source.g * pattern, source.b * pattern, 1f)
                    : new Color(pattern, pattern, pattern, 1f);
                objectPixels[index] = objectColor;

                // Centering by (-1)^(x+y) places the zero order in the texture centre.
                float centeringSign = ((x + y) & 1) == 0 ? 1f : -1f;
                real[0][index] = Mathf.Sqrt(Mathf.Max(0f, objectColor.r)) * centeringSign;
                real[1][index] = Mathf.Sqrt(Mathf.Max(0f, objectColor.g)) * centeringSign;
                real[2][index] = Mathf.Sqrt(Mathf.Max(0f, objectColor.b)) * centeringSign;
                imaginary[0][index] = 0f;
                imaginary[1][index] = 0f;
                imaginary[2][index] = 0f;
            }
        }
    }

    private static float EvaluateLineGrating(int x, int size, float cycles)
    {
        float phase = (x + 0.5f) / size * cycles;
        return Mathf.Repeat(phase, 1f) < 0.5f ? 1f : 0f;
    }

    private void BuildFourierPlane(bool useLaserExcitation, bool hasSpecimen, int size)
    {
        float maximum = 0f;
        for (int i = 0; i < spectrumPixels.Length; i++)
        {
            float magnitude = ChannelMagnitude(0, i) + ChannelMagnitude(1, i) + ChannelMagnitude(2, i);
            maximum = Mathf.Max(maximum, Mathf.Log(1f + magnitude));
        }

        float inverseMaximum = maximum > 0f ? 1f / maximum : 1f;
        Color laserTint = useLaserExcitation
            ? hasSpecimen ? AverageObjectTint() : new Color(1f, 0.08f, 0.035f, 1f)
            : Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                Color spectrum;
                if (useLaserExcitation)
                {
                    float value = Mathf.Log(1f +
                        ChannelMagnitude(0, index) +
                        ChannelMagnitude(1, index) +
                        ChannelMagnitude(2, index)) * inverseMaximum;
                    spectrum = laserTint * Mathf.Pow(value, 0.42f);
                }
                else
                {
                    float red = SampleScaledSpectrum(0, x, y, size, 550f / 650f);
                    float green = SampleScaledSpectrum(1, x, y, size, 1f);
                    float blue = SampleScaledSpectrum(2, x, y, size, 550f / 450f);
                    spectrum = new Color(red, green, blue) * inverseMaximum;
                    spectrum.r = Mathf.Pow(Mathf.Clamp01(spectrum.r), 0.42f);
                    spectrum.g = Mathf.Pow(Mathf.Clamp01(spectrum.g), 0.42f);
                    spectrum.b = Mathf.Pow(Mathf.Clamp01(spectrum.b), 0.42f);
                }

                spectrumPixels[index] = new Color(
                    Mathf.Clamp01(spectrum.r),
                    Mathf.Clamp01(spectrum.g),
                    Mathf.Clamp01(spectrum.b),
                    1f);
            }
        }
    }

    private Color AverageObjectTint()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        int step = Mathf.Max(1, objectPixels.Length / 512);
        for (int i = 0; i < objectPixels.Length; i += step)
        {
            Color color = objectPixels[i];
            if (color.maxColorComponent < 0.02f)
            {
                continue;
            }

            sum += new Vector3(color.r, color.g, color.b);
            count++;
        }

        if (count == 0)
        {
            return new Color(1f, 0.18f, 0.08f, 1f);
        }

        Vector3 average = sum / count;
        float maximum = Mathf.Max(average.x, Mathf.Max(average.y, average.z));
        if (maximum < 0.05f)
        {
            return new Color(1f, 0.18f, 0.08f, 1f);
        }

        average /= maximum;
        return new Color(average.x, average.y, average.z, 1f);
    }

    private float SampleScaledSpectrum(int channel, int x, int y, int size, float scale)
    {
        float centre = (size - 1) * 0.5f;
        int sampleX = Mathf.Clamp(Mathf.RoundToInt(centre + (x - centre) * scale), 0, size - 1);
        int sampleY = Mathf.Clamp(Mathf.RoundToInt(centre + (y - centre) * scale), 0, size - 1);
        return Mathf.Log(1f + ChannelMagnitude(channel, sampleY * size + sampleX));
    }

    private float ChannelMagnitude(int channel, int index)
    {
        float realValue = real[channel][index];
        float imaginaryValue = imaginary[channel][index];
        return Mathf.Sqrt(realValue * realValue + imaginaryValue * imaginaryValue);
    }

    private void ApplyObjectivePupil(
        bool hasSpecimen,
        float normalizedFrequency,
        int visibleOrderCount,
        int size)
    {
        float centre = (size - 1) * 0.5f;
        float specimenCutoff = Mathf.Lerp(0.30f, 0.105f, normalizedFrequency);
        float gratingCycles = Mathf.Lerp(6f, 24f, normalizedFrequency);
        float gratingCutoff = (visibleOrderCount + 0.35f) * gratingCycles / size;
        float cutoff = hasSpecimen
            ? specimenCutoff
            : Mathf.Clamp(gratingCutoff, 0.08f, 0.46f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                float normalizedX = (x - centre) / size;
                float normalizedY = (y - centre) / size;
                float radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                float edge = Mathf.Clamp01((cutoff - radius) * size * 0.75f);
                float transmission = edge * edge * (3f - 2f * edge);

                for (int channel = 0; channel < ChannelCount; channel++)
                {
                    real[channel][index] *= transmission;
                    imaginary[channel][index] *= transmission;
                }
            }
        }
    }

    private void BuildReconstruction(int size)
    {
        float maximum = 0f;
        for (int i = 0; i < reconstructionPixels.Length; i++)
        {
            float red = ChannelIntensity(0, i);
            float green = ChannelIntensity(1, i);
            float blue = ChannelIntensity(2, i);
            maximum = Mathf.Max(maximum, Mathf.Max(red, Mathf.Max(green, blue)));
        }

        float inverseMaximum = maximum > 0f ? 1f / maximum : 1f;
        for (int i = 0; i < reconstructionPixels.Length; i++)
        {
            Color color = new Color(
                Mathf.Sqrt(ChannelIntensity(0, i) * inverseMaximum),
                Mathf.Sqrt(ChannelIntensity(1, i) * inverseMaximum),
                Mathf.Sqrt(ChannelIntensity(2, i) * inverseMaximum),
                1f);
            reconstructionPixels[i] = color;
        }
    }

    private float ChannelIntensity(int channel, int index)
    {
        float realValue = real[channel][index];
        float imaginaryValue = imaginary[channel][index];
        return realValue * realValue + imaginaryValue * imaginaryValue;
    }

    private void UploadTextures()
    {
        objectPlaneTexture.SetPixels32(objectPixels);
        objectPlaneTexture.Apply(false, false);
        fourierPlaneTexture.SetPixels32(spectrumPixels);
        fourierPlaneTexture.Apply(false, false);
        reconstructedImageTexture.SetPixels32(reconstructionPixels);
        reconstructedImageTexture.Apply(false, false);
    }

    private static void Fft2D(float[] realValues, float[] imaginaryValues, int size, bool inverse)
    {
        for (int row = 0; row < size; row++)
        {
            Fft1D(realValues, imaginaryValues, row * size, 1, size, inverse);
        }

        for (int column = 0; column < size; column++)
        {
            Fft1D(realValues, imaginaryValues, column, size, size, inverse);
        }
    }

    private static void Fft1D(
        float[] realValues,
        float[] imaginaryValues,
        int offset,
        int stride,
        int length,
        bool inverse)
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
            float angle = (inverse ? 2f : -2f) * Mathf.PI / blockLength;
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

        if (!inverse)
        {
            return;
        }

        float inverseLength = 1f / length;
        for (int index = 0; index < length; index++)
        {
            int target = offset + index * stride;
            realValues[target] *= inverseLength;
            imaginaryValues[target] *= inverseLength;
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
        if (objectPlaneTexture != null)
        {
            Destroy(objectPlaneTexture);
        }

        if (fourierPlaneTexture != null)
        {
            Destroy(fourierPlaneTexture);
        }

        if (reconstructedImageTexture != null)
        {
            Destroy(reconstructedImageTexture);
        }

        objectPlaneTexture = null;
        fourierPlaneTexture = null;
        reconstructedImageTexture = null;
    }
}
