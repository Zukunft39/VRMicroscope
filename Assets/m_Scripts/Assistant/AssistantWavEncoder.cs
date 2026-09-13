using System;
using System.IO;
using System.Text;

namespace VRMicroscope.Assistant
{
    // No Unity dependency: encode only recorded frames, downmix channels, preserve sample rate.
    public static class AssistantWavEncoder
    {
        public static byte[] Encode(float[] samples, int frames, int channels, int frequency)
        {
            if (samples == null || channels < 1 || channels > 8 || frequency < 8000 || frequency > 48000 ||
                frames < frequency / 2 || frames > frequency * 30 || (long)frames * channels > samples.Length)
                throw new ArgumentException("Invalid recording length or format.");
            using (var stream = new MemoryStream(44 + frames * 2))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + frames * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(frequency);
                writer.Write(frequency * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(frames * 2);
                for (int frame = 0; frame < frames; frame++)
                {
                    double sample = 0;
                    for (int channel = 0; channel < channels; channel++)
                        sample += samples[frame * channels + channel];
                    sample /= channels;
                    if (double.IsNaN(sample) || double.IsInfinity(sample)) sample = 0;
                    writer.Write((short)Math.Round(Math.Max(-1, Math.Min(1, sample)) * 32767));
                }
                return stream.ToArray();
            }
        }
    }
}
