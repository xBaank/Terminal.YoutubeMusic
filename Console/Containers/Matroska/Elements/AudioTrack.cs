namespace Console.Containers.Matroska.Elements;

using Types;

internal readonly record struct AudioTrack(
    AudioCodecTypes Codec,
    AudioSettings Settings,
    int Number
);

internal readonly record struct AudioSettings(double SamplingRate, int Channels, int BitDepth);
