using System.Text;

namespace Console.Extensions;

internal static class StringExtensions
{
    public static string ToASCII(this string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return Encoding.ASCII.GetString(bytes);
    }
}
