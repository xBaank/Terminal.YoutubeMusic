namespace Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;

public static class MemoryExtensions
{
    public static ReadOnlyMemory<byte> AsReadOnlyMemory(this Memory<byte> source) => source;
}
