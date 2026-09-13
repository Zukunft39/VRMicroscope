using System;
using System.IO;
using System.Text;
using VRMicroscope.Assistant;

internal static class WavChecks
{
    public static void Run()
    {
        // Include an unused tail: microphone buffer capacity must not become recorded duration.
        var samples = new float[32000];
        samples[0]=1; samples[1]=0; samples[2]=-1; samples[3]=-1;
        var data=AssistantWavEncoder.Encode(samples,8000,2,16000);
        using(var reader=new BinaryReader(new MemoryStream(data)))
        {
            Check(Encoding.ASCII.GetString(reader.ReadBytes(4))=="RIFF", "RIFF header");
            Check(reader.ReadInt32()==16036,"RIFF length");
            Check(Encoding.ASCII.GetString(reader.ReadBytes(8))=="WAVEfmt ","WAVE format");
            Check(reader.ReadInt32()==16 && reader.ReadInt16()==1 && reader.ReadInt16()==1,"PCM mono");
            Check(reader.ReadInt32()==16000 && reader.ReadInt32()==32000,"Sample/byte rate");
            Check(reader.ReadInt16()==2 && reader.ReadInt16()==16,"Sample width");
            Check(Encoding.ASCII.GetString(reader.ReadBytes(4))=="data" && reader.ReadInt32()==16000,"Actual frame count");
            Check(reader.ReadInt16()==16384 && reader.ReadInt16()==-32767,"Stereo downmix");
        }
        Check(data.Length==16044,"No unrecorded buffer tail");
        var invalid=new float[8000]; invalid[0]=float.NaN; invalid[1]=float.PositiveInfinity; invalid[2]=2;
        var sanitized=AssistantWavEncoder.Encode(invalid,8000,1,16000);
        Check(BitConverter.ToInt16(sanitized,44)==0 && BitConverter.ToInt16(sanitized,46)==0 &&
            BitConverter.ToInt16(sanitized,48)==32767,"Nonfinite and clipping");
        bool rejected=false;
        try { AssistantWavEncoder.Encode(invalid,8001,1,16000); } catch(ArgumentException) { rejected=true; }
        Check(rejected,"Invalid array bounds");
        rejected=false;
        try { AssistantWavEncoder.Encode(invalid,7999,1,16000); } catch(ArgumentException) { rejected=true; }
        Check(rejected,"Minimum duration");
        Console.WriteLine("Voice WAV: 12 encoding checks passed.");
    }
    private static void Check(bool value,string message) { if(!value) throw new Exception(message); }
}
