using System.Text;

namespace Console.Extensions;

internal static class StringExtensions
{
    // Temp fix for rare unicode chars that crash the application
    public static string ToASCII(this string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return Encoding.ASCII.GetString(bytes);
    }
}
