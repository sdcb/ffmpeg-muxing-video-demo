using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;

FFmpegLogger.LogWriter = (l, m) => Console.Write($"[{l}] {m}");
string sourceFolder = @".\src\%03d.jpg";
AVRational frameRate = new(10, 1);
string outputFile = "output.mp4";
long bitRate = 2 * 1024 * 1024; // 2M

using FormatContext srcFc = FormatContext.OpenInputUrl(sourceFolder, options: new MediaDictionary
{
    ["framerate"] = frameRate.ToString()
});
srcFc.LoadStreamInfo();
MediaStream srcVideo = srcFc.GetVideoStream();
CodecParameters srcCodecParameters = srcVideo.Codecpar!;
using CodecContext videoDecoder = new(Codec.FindDecoderById(srcCodecParameters.CodecId))
{
};
videoDecoder.FillParameters(srcCodecParameters);
videoDecoder.Open();


using FormatContext dstFc = FormatContext.AllocOutput(OutputFormat.Guess("mp4"));
dstFc.VideoCodec = Codec.CommonEncoders.Libx264;
MediaStream vstream = dstFc.NewStream(dstFc.VideoCodec);
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

using IOContext io = IOContext.OpenWrite(outputFile);
dstFc.Pb = io;
dstFc.WriteHeader();

// srcFc.ReadPackets() -- stream ->
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