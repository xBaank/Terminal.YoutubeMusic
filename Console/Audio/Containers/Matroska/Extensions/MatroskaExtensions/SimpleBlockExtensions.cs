namespace Console.Audio.Containers.Matroska.Extensions.MatroskaExtensions;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Extensions;
using Console.Audio.Containers.Matroska.Types;

//TODO support lacing
internal static class SimpleBlockExtensions
{
    public static IEnumerable<ReadOnlyMemory<byte>> GetFrames(this SimpleBlock block) =>
        block.LacingType switch
        {
            LacingType.NoLacing => [block.FramesData],
            LacingType.Xiph => block.GetXiphFrames(),
            LacingType.FixedSize => block.GetFixedSizeFrames(),
            LacingType.Ebml => block.GetEbmlFrames(),
            _ => throw new ArgumentOutOfRangeException(nameof(block), block.LacingType, null),
        };

    private static IEnumerable<ReadOnlyMemory<byte>> GetXiphFrames(this SimpleBlock block)
    {
        var head = block.FramesData[4..].Span;
        var numberOfFrames = head[0];
        var vint = EbmlUtils.GetVint(true, head[1..]);
        var size = vint.ToLong();
        var sizeOfVint = vint.Length;

        var framesData = head[(sizeOfVint + 1)..];

        throw new NotImplementedException();
    }

    private static IEnumerable<ReadOnlyMemory<byte>> GetFixedSizeFrames(this SimpleBlock block)
    {
        var head = block.FramesData[4..].Span;
        var numberOfFrames = head[0];
        //We don't need to read the size of the frames because they are all the same size
        var sizeOfVint = EbmlUtils.GetVint(true, head[1..]).Length;

        var framesData = head[(sizeOfVint + 1)..];

        throw new NotImplementedException();
    }

    private static IEnumerable<ReadOnlyMemory<byte>> GetEbmlFrames(this SimpleBlock block)
    {
        var head = block.FramesData[4..].Span;
        var numberOfFrames = head[0];
        var vint = EbmlUtils.GetVint(true, head[1..]);
        var size = vint.ToLong();
        var sizeOfVint = vint.Length;

        var framesData = head[(sizeOfVint + 1)..];

        throw new NotImplementedException();
    }
}
