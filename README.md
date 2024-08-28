# ffmpeg-muxing-video

This demo shows how to use the C# Sdcb.FFmpeg library to mux a video from an image sequence.

## Overview

This project demonstrates how to convert a sequence of images into an MP4 video using the Sdcb.FFmpeg library in C#. The code is structured to read images from a specified folder, decode them, and encode them into an MP4 file. Below are the key configurations you can adjust to customize the conversion process.

## Key Configurations

1. **Source Folder**: The folder containing the image sequence.
   ```csharp
   string sourceFolder = @".\src\%03d.jpg";
   ```
   - This string specifies the folder and naming pattern of the source images. The `%03d` part denotes that the images are named with three-digit numbers (e.g., 001.jpg, 002.jpg, etc.).

2. **Frame Rate**: The frame rate for the output video.
   ```csharp
   AVRational frameRate = new(10, 1);
   ```
   - This defines the frame rate of the video. In this example, the frame rate is set to 10 frames per second.

3. **Output File**: The name of the output video file.
   ```csharp
   string outputFile = "output.mp4";
   ```
   - This specifies the name of the generated MP4 file.

4. **Bit Rate**: The bit rate for the output video.
   ```csharp
   long bitRate = 2 * 1024 * 1024; // 2M
   ```
   - This sets the bit rate of the output video, ensuring a balance between video quality and file size. Here, the bit rate is set to 2 Mbps.

## Step-by-Step Process

1. **Initialize Logging**:
   ```csharp
   FFmpegLogger.LogWriter = (l, m) => Console.Write($"[{l}] {m}");
   ```

2. **Open Source FormatContext**:
   ```csharp
   using FormatContext srcFc = FormatContext.OpenInputUrl(sourceFolder, options: new MediaDictionary
   {
       ["framerate"] = frameRate.ToString()
   });
   srcFc.LoadStreamInfo();
   ```

3. **Get Source Video Stream and Codec Parameters**:
   ```csharp
   MediaStream srcVideo = srcFc.GetVideoStream();
   CodecParameters srcCodecParameters = srcVideo.Codecpar!;
   ```

4. **Initialize Decoder**:
   ```csharp
   using CodecContext videoDecoder = new(Codec.FindDecoderById(srcCodecParameters.CodecId))
   {
   };
   videoDecoder.FillParameters(srcCodecParameters);
   videoDecoder.Open();
   ```

5. **Allocate Output FormatContext**:
   ```csharp
   using FormatContext dstFc = FormatContext.AllocOutput(OutputFormat.Guess("mp4"));
   dstFc.VideoCodec = Codec.CommonEncoders.Libx264;
   MediaStream vstream = dstFc.NewStream(dstFc.VideoCodec);
   ```

6. **Initialize Encoder**:
   ```csharp
   using CodecContext vcodec = new(dstFc.VideoCodec)
   {
       Width = srcCodecParameters.Width,
       Height = srcCodecParameters.Height,
       TimeBase = frameRate.Inverse(),
       PixelFormat = AVPixelFormat.Yuv420p,
       Flags = AV_CODEC_FLAG.GlobalHeader,
       BitRate = bitRate, 
   };
   vcodec.Open(dstFc.VideoCodec);
   vstream.Codecpar!.CopyFrom(vcodec);
   vstream.TimeBase = vcodec.TimeBase;
   ```

7. **Open IO Context and Write Header**:
   ```csharp
   using IOContext io = IOContext.OpenWrite(outputFile);
   dstFc.Pb = io;
   dstFc.WriteHeader();
   ```

8. **Read, Decode, Convert, Encode, and Write Packets**:
   ```csharp
   foreach (Packet packet in srcFc
       .ReadPackets().Where(x => x.StreamIndex == srcVideo.Index)
       .DecodePackets(videoDecoder)
       .ConvertFrames(vcodec)
       .EncodeFrames(vcodec)
       )
   {
       try
       {
           packet.RescaleTimestamp(vcodec.TimeBase, vstream.TimeBase);
           packet.StreamIndex = vstream.Index;
           dstFc.InterleavedWritePacket(packet);
       }
       finally
       {
           packet.Unref();
       }
   }
   dstFc.WriteTrailer();
   ```

## Dependencies

- Sdcb.FFmpeg library

## How to Run

1. Ensure you have the Sdcb.FFmpeg library installed.
2. Place your image sequence in the specified source folder (e.g., `.\src\` with names like `001.jpg`, `002.jpg`, etc.).
3. Adjust the key configurations if necessary.
4. Run the code to generate the `output.mp4` file.

This project is a practical example of how to use the Sdcb.FFmpeg library to create a video from a series of images. Adjust the configurations as needed to suit your specific requirements.