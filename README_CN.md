# ��ͼ������ת��ΪMP4��ʽ����Ƶ��ʹ��C# Sdcb.FFmpeg��

**[English](README.md)** | **��������**

��ʾ����ʾ�����ʹ��C# Sdcb.FFmpeg�⽫ͼ�����кϳ���Ƶ��

## ����

����Ŀչʾ�����ʹ��C#�е�Sdcb.FFmpeg�⽫һϵ��ͼ��ת����MP4��Ƶ������ṹ���ش�ָ���ļ��ж�ȡͼ�񣬽��벢��������ΪMP4�ļ�������Ŀʹ���� `Sdcb.FFmpeg` �⣬������ [����](https://github.com/sdcb/Sdcb.FFmpeg) �ҵ���

## �ؼ�����

1. **Դ�ļ���**: ����ͼ�����е��ļ��С�
   ```csharp
   string sourceFolder = @".\src\%03d.jpg";
   ```
   - ���ַ���ָ����Դͼ����ļ��к�����ģʽ��`%03d`���ֱ�ʾͼ������λ�������������磬001.jpg��002.jpg�ȣ���

2. **֡��**: �����Ƶ��֡�ʡ�
   ```csharp
   AVRational frameRate = new(10, 1);
   ```
   - �ⶨ������Ƶ��֡�ʡ��ڴ�ʾ���У�֡������Ϊÿ��10֡��

3. **����ļ�**: �����Ƶ�ļ������ơ�
   ```csharp
   string outputFile = "output.mp4";
   ```
   - ��ָ�������ɵ�MP4�ļ������ơ�

4. **������**: �����Ƶ�ı����ʡ�
   ```csharp
   long bitRate = 2 * 1024 * 1024; // 2M
   ```
   - �������������Ƶ�ı����ʣ�ȷ����Ƶ�������ļ���С֮���ƽ�⡣���������������Ϊ2Mbps��

## ��������

1. **��ʼ����־��¼**:
   ```csharp
   FFmpegLogger.LogWriter = (l, m) => Console.Write($"[{l}] {m}");
   ```

2. **��ԴFormatContext**:
   ```csharp
   using FormatContext srcFc = FormatContext.OpenInputUrl(sourceFolder, options: new MediaDictionary
   {
       ["framerate"] = frameRate.ToString()
   });
   srcFc.LoadStreamInfo();
   ```

3. **��ȡԴ��Ƶ���ͱ������**:
   ```csharp
   MediaStream srcVideo = srcFc.GetVideoStream();
   CodecParameters srcCodecParameters = srcVideo.Codecpar!;
   ```

4. **��ʼ��������**:
   ```csharp
   using CodecContext videoDecoder = new(Codec.FindDecoderById(srcCodecParameters.CodecId))
   {
   };
   videoDecoder.FillParameters(srcCodecParameters);
   videoDecoder.Open();
   ```

5. **�������FormatContext**:
   ```csharp
   using FormatContext dstFc = FormatContext.AllocOutput(OutputFormat.Guess("mp4"));
   dstFc.VideoCodec = Codec.CommonEncoders.Libx264;
   MediaStream vstream = dstFc.NewStream(dstFc.VideoCodec);
   ```

6. **��ʼ��������**:
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

7. **��IO Context��д��ͷ��**:
   ```csharp
   using IOContext io = IOContext.OpenWrite(outputFile);
   dstFc.Pb = io;
   dstFc.WriteHeader();
   ```

8. **��ȡ�����롢ת�������벢д���**:
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

## ������

- Sdcb.FFmpeg�⣬������ [����](https://github.com/sdcb/Sdcb.FFmpeg) �ҵ���

## ���в���

1. ȷ���Ѱ�װSdcb.FFmpeg�⡣
2. ��ͼ�����з�����ָ����Դ�ļ����У����� `.\src\`�������� `001.jpg`��`002.jpg`�ȣ���
3. ������Ҫ�������ؼ����á�
4. ���д��������� `output.mp4` �ļ���

����Ŀ��һ��ʹ��Sdcb.FFmpeg���һϵ��ͼ�񴴽���Ƶ��ʵ��ʾ�������ݾ�������������á�