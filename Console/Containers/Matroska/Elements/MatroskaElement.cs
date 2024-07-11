namespace Console.Containers.Matroska.Elements;

using Console.Containers.Matroska.EBML;

internal readonly record struct MatroskaElement(VInt Id, long Size, long Position);
