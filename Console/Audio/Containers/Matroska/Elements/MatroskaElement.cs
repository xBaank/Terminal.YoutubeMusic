namespace Console.Audio.Containers.Matroska.Elements;

using Console.Audio.Containers.Matroska.EBML;

internal readonly record struct MatroskaElement(VInt Id, long Size, long Position);
