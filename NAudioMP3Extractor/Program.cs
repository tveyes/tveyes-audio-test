using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using NAudio.Lame;
using NAudio.Wave;

namespace NAudioMP3Extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            //First off, lets get a URL to work with:
            String audioMp3File = "sample.mp3";
            
            //Read the file into a byte[]
            Byte[] audioMp3Data = File.ReadAllBytes(audioMp3File);
            Console.WriteLine("Audio MP3 Data Size: {0}", audioMp3Data.Length);


            //Now lets see if we can convert this byte[] in a wave byte[]
            Byte[] audioWaveData = MP3BytesToWavBytes(audioMp3Data);
            File.WriteAllBytes("output.wav", audioWaveData);
            Console.WriteLine("Audio Wav Data Size: {0}", audioWaveData.Length);

            //Now lets trim
            Byte[] trimmedAudio = ExtractRegionFromWavBytes(audioWaveData, new TimeSpan(0, 0, 20), new TimeSpan(0, 0, 40));
            File.WriteAllBytes("output_trimmed.wav", trimmedAudio);
            Console.WriteLine("Audio Wav Trimmed Data Size: {0}", trimmedAudio.Length);

        }
        
        private static byte[] MP3BytesToWavBytes(byte[] input, WaveFormat outputFormat = null)
        {
            byte[] retval = null;

            using (MemoryStream outputStream = new MemoryStream()) 
            using (MemoryStream inputStream = new MemoryStream(input))
            using (WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(inputStream)))
            {
                if (outputFormat == null)
                    outputFormat = waveStream.WaveFormat;
                using (WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, outputFormat))
                {
                    byte[] bytes = new byte[waveStream.Length];
                    waveStream.Position = 0;
                    waveStream.Read(bytes, 0, (int)waveStream.Length);
                    waveFileWriter.Write(bytes, 0, bytes.Length);
                    waveFileWriter.Flush();

                    outputStream.Position = 0;
                    retval = outputStream.ToArray();
                }
            }
            return retval;
        }


        private static byte[] ExtractRegionFromWavBytes(byte[] audioWavData, TimeSpan startTime, TimeSpan endTime)
        {
            byte[] outputWavSegment = null;

            using (MemoryStream msreader = new MemoryStream(audioWavData))
            {
                using (MemoryStream mswriter = new MemoryStream())
                {
                    WaveFileReader wfReader = new WaveFileReader(msreader);
                    WaveFileWriter wfWriter = new WaveFileWriter(mswriter, wfReader.WaveFormat);
                    TrimWavFile(wfReader, wfWriter, startTime, endTime);
                    
                    mswriter.Position = 0;
                    outputWavSegment = new byte[mswriter.Length];
                    mswriter.Read(outputWavSegment, 0, outputWavSegment.Length);
                }
            }

            return outputWavSegment;
        }




        public static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, TimeSpan startTime, TimeSpan endTime)
        {
            int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

            int startPos = (int)startTime.TotalMilliseconds * bytesPerMillisecond;
            startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

            int endPos = (int)endTime.TotalMilliseconds * bytesPerMillisecond;
            endPos = endPos - endPos % reader.WaveFormat.BlockAlign;

            TrimWavFile(reader, writer, startPos, endPos);
        }
        private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.WriteData(buffer, 0, bytesRead);
                    }
                }
            }
            writer.Flush();
        }


    }
}
