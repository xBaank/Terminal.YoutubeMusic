namespace Console.Audio.Containers.Matroska.Types;

using Console.Audio.Containers.Matroska.EBML;
using static DataType;

public enum DataType
{
    Master,
    UnsignedInteger,
    SignedInteger,
    String,
    Utf8String,
    Binary,
    Float,
    Date,
}

//Only time I wished this was java
internal static class ElementTypes
{
    public static readonly ElementType EbmlHeader =
        new(Master, new byte[] { 0x1A, 0x45, 0xDF, 0xA3 });
    public static readonly ElementType DocType = new(String, new byte[] { 0x42, 0x82 });
    public static readonly ElementType Segment = new(Master, new byte[] { 0x18, 0x53, 0x80, 0x67 });
    public static readonly ElementType Tracks = new(Master, new byte[] { 0x16, 0x54, 0xAE, 0x6B });
    public static readonly ElementType Chapters =
        new(Master, new byte[] { 0x10, 0x43, 0xA7, 0x70 });
    public static readonly ElementType TrackEntry = new(Master, new byte[] { 0xAE });
    public static readonly ElementType TrackNumber = new(UnsignedInteger, new byte[] { 0xD7 });
    public static readonly ElementType TrackType = new(UnsignedInteger, new byte[] { 0x83 });
    public static readonly ElementType CodecId = new(String, new byte[] { 0x86 });
    public static readonly ElementType Audio = new(Master, new byte[] { 0xE1 });
    public static readonly ElementType Sampling = new(Float, new byte[] { 0xB5 });
    public static readonly ElementType Channels = new(UnsignedInteger, new byte[] { 0x9F });
    public static readonly ElementType BitDepth = new(UnsignedInteger, new byte[] { 0x62, 0x64 });
    public static readonly ElementType Cluster = new(Master, new byte[] { 0x1f, 0x43, 0xb6, 0x75 });
    public static readonly ElementType Timestamp = new(Binary, new byte[] { 0xE7 });
    public static readonly ElementType SimpleBlock = new(Binary, new byte[] { 0xA3 });
    public static readonly ElementType SeekHead =
        new(Master, new byte[] { 0x11, 0x4D, 0x9B, 0x74 });
    public static readonly ElementType Seek = new(Master, new byte[] { 0x4D, 0xBB });
    public static readonly ElementType SeekId = new(Binary, new byte[] { 0x53, 0xAB });
    public static readonly ElementType SeekPos = new(UnsignedInteger, new byte[] { 0x53, 0xAC });
    public static readonly ElementType Cues = new(Master, new byte[] { 0x1C, 0x53, 0xBB, 0x6B });
    public static readonly ElementType CuePoint = new(Master, new byte[] { 0xBB });
    public static readonly ElementType CueTime = new(UnsignedInteger, new byte[] { 0xB3 });
    public static readonly ElementType CueTrackPositions = new(Master, new byte[] { 0xB7 });
    public static readonly ElementType CueTrack = new(UnsignedInteger, new byte[] { 0xF7 });
    public static readonly ElementType CueClusterPosition =
        new(UnsignedInteger, new byte[] { 0xF1 });
    public static readonly ElementType TimestampScale =
        new(UnsignedInteger, new byte[] { 0x2A, 0xD7, 0xB1 });
    public static readonly ElementType Duration = new(Float, new byte[] { 0x44, 0x89 });
    public static readonly ElementType Info = new(Master, new byte[] { 0x15, 0x49, 0xA9, 0x66 });
    public static readonly ElementType Attachments =
        new(Master, new byte[] { 0x19, 0x41, 0xA4, 0x69 });
    public static readonly ElementType Tags = new(Master, new byte[] { 0x12, 0x54, 0xC3, 0x67 });

    //Array with all the element types
    public static readonly ElementType[] All =
    {
        EbmlHeader,
        DocType,
        Segment,
        Tracks,
        TrackEntry,
        CodecId,
        Audio,
        Sampling,
        Channels,
        Cluster,
        Timestamp,
        SimpleBlock,
        Cues,
        CuePoint,
        CueTime,
        CueTrackPositions,
        CueTrack,
        CueClusterPosition,
        TimestampScale,
        Duration,
        Info,
        SeekHead,
        Seek,
        SeekId,
        SeekPos,
        TrackNumber,
        TrackType,
        BitDepth,
        Chapters,
    };

    public static readonly ElementType[] TopElements =
    {
        SeekHead,
        Info,
        Tracks,
        Chapters,
        Cues,
        Cluster,
        Attachments,
        Tags,
    };

    internal static readonly VInt[] TopElementsIds = TopElements.Select(x => x.Id).ToArray();
}

internal readonly record struct ElementType(DataType DataType, VInt Id);
