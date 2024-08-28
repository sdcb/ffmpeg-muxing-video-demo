# 将图像序列转换为MP4格式的视频（使用C# Sdcb.FFmpeg）

**[English](README.md)** | **简体中文**

本示例演示了如何使用C# Sdcb.FFmpeg库将图像序列合成视频。

## 概述

该项目展示了如何使用C#中的Sdcb.FFmpeg库将一系列图像转换成MP4视频。代码结构化地从指定文件夹读取图像，解码并编码它们为MP4文件。该项目使用了 `Sdcb.FFmpeg` 库，可以在 [这里](https://github.com/sdcb/Sdcb.FFmpeg) 找到。

## 关键配置

1. **源文件夹**: 包含图像序列的文件夹。
   ```csharp
   string sourceFolder = @".\src\%03d.jpg";
   ```
   - 该字符串指定了源图像的文件夹和命名模式。`%03d`部分表示图像以三位数字命名（例如，001.jpg，002.jpg等）。

2. **帧率**: 输出视频的帧率。
   ```csharp
   AVRational frameRate = new(10, 1);
   ```
   - 这定义了视频的帧率。在此示例中，帧率设置为每秒10帧。

3. **输出文件**: 输出视频文件的名称。
   ```csharp
   string outputFile = "output.mp4";
   ```
   - 这指定了生成的MP4文件的名称。

4. **比特率**: 输出视频的比特率。
   ```csharp
   long bitRate = 2 * 1024 * 1024; // 2M
   ```
   - 这设置了输出视频的比特率，确保视频质量与文件大小之间的平衡。在这里，比特率设置为2Mbps。

## 步骤流程

1. **初始化日志记录**:
   ```csharp
   FFmpegLogger.LogWriter = (l, m) => Console.Write($"[{l}] {m}");
   ```

2. **打开源FormatContext**:
   ```csharp
   using FormatContext srcFc = FormatContext.OpenInputUrl(sourceFolder, options: new MediaDictionary
   {
       ["framerate"] = frameRate.ToString()
   });
   srcFc.LoadStreamInfo();
   ```

3. **获取源视频流和编码参数**:
   ```csharp
   MediaStream srcVideo = srcFc.GetVideoStream();
   CodecParameters srcCodecParameters = srcVideo.Codecpar!;
   ```

4. **初始化解码器**:
   ```csharp
   using CodecContext videoDecoder = new(Codec.FindDecoderById(srcCodecParameters.CodecId))
   {
   };
   videoDecoder.FillParameters(srcCodecParameters);
   videoDecoder.Open();
   ```

5. **分配输出FormatContext**:
   ```csharp
   using FormatContext dstFc = FormatContext.AllocOutput(OutputFormat.Guess("mp4"));
   dstFc.VideoCodec = Codec.CommonEncoders.Libx264;
   MediaStream vstream = dstFc.NewStream(dstFc.VideoCodec);
   ```

6. **初始化编码器**:
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

7. **打开IO Context并写入头部**:
   ```csharp
   using IOContext io = IOContext.OpenWrite(outputFile);
   dstFc.Pb = io;
   dstFc.WriteHeader();
   ```

8. **读取、解码、转换、编码并写入包**:
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

## 依赖项

- Sdcb.FFmpeg库，可以在 [这里](https://github.com/sdcb/Sdcb.FFmpeg) 找到。

## 运行步骤

1. 确保已安装Sdcb.FFmpeg库。
2. 将图像序列放置在指定的源文件夹中（例如 `.\src\`，名称如 `001.jpg`、`002.jpg`等）。
3. 如有需要，调整关键配置。
4. 运行代码以生成 `output.mp4` 文件。

该项目是一个使用Sdcb.FFmpeg库从一系列图像创建视频的实用示例。根据具体需求调整配置。