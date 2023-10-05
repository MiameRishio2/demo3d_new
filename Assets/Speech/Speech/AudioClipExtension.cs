using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave;
using UnityEngine;

public static class AudioClipExtension
{
    /// <summary>
    /// 转化为PCM16bit数据
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static byte[] ToPCM16(this AudioClip self)
    {
        var samples = new float[self.samples * self.channels];
        self.GetData(samples, 0);
        var samples_int16 = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            var f = samples[i];
            samples_int16[i] = (short)(f * short.MaxValue);
        }
        var retBytes = new byte[samples_int16.Length * 2];
        Buffer.BlockCopy(samples_int16, 0, retBytes, 0, retBytes.Length);
        return retBytes;
    }
 
    /// <summary>
    /// 字节数组转AudioClip音频
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static AudioClip ToWAV(this byte[] self)
    {
        // 转换mp3格式的代码
        //MemoryStream mp3stream = new MemoryStream(buffer);
        // Convert the data in the stream to WAV format
        //Mp3FileReader mp3audio = new Mp3FileReader(mp3stream);
 
        //转换wave格式的代码
        MemoryStream wavstream = new MemoryStream(self);
        WaveFileReader waveAudio = new WaveFileReader(wavstream);
 
        WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(waveAudio);
        MemoryStream outputStream = new MemoryStream();
        using (WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat))
        {
            byte[] bytes = new byte[waveStream.Length];
            waveStream.Position = 0;
            waveStream.Read(bytes, 0, Convert.ToInt32(waveStream.Length));
            waveFileWriter.Write(bytes, 0, bytes.Length);
            waveFileWriter.Flush();
        }
        // Convert to WAV data
        WAV wav = new WAV(outputStream.ToArray());
        AudioClip audioClip = AudioClip.Create("new audio clip", wav.SampleCount, 1, wav.Frequency, false);
        audioClip.SetData(wav.LeftChannel, 0);
        return audioClip;
}
    
    /// <summary>
    /// 结构体转字节数组
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static byte[] ToBytes(this object self)
    {
        int size = Marshal.SizeOf(self);
        IntPtr intPtr = Marshal.AllocHGlobal(size);
        byte[] retValue;
        try
        {
            Marshal.StructureToPtr(self, intPtr, false);
            byte[] array = new byte[size];
            Marshal.Copy(intPtr, array, 0, size);
            retValue = array;
        }
        finally
        {
            Marshal.FreeHGlobal(intPtr);
        }
        return retValue;
    }
}