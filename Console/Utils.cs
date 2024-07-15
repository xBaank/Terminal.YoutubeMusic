using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace Console;

internal static class Utils
{
    public static void ConfigurePlatformDependencies()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            OpenALLibraryNameContainer.OverridePath = "libopenal.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            OpenALLibraryNameContainer.OverridePath = "libopenal.dylib";
        }
        else
        {
            OpenALLibraryNameContainer.OverridePath = "soft_oal.dll";
        }
    }
}
