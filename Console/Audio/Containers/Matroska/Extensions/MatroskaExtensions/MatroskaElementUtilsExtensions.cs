namespace Console.Audio.Containers.Matroska.Extensions.MatroskaExtensions;

using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Types;

internal static class MatroskaElementUtilsExtensions
{
    public static MatroskaElement? As(this MatroskaElement element, ElementType type) =>
        element.Id == type.Id ? element : null;
}
