﻿using System.Text;
using System.Web;

namespace Console.Extensions;

internal static class StringExtensions
{
    // Temp fix for rare unicode chars that crash the application
    public static string ToASCII(this object obj)
    {
        var str = obj.ToString();

        if (str == null)
            return "";

        var bytes = Encoding.UTF8.GetBytes(str.ToString());
        return Encoding.ASCII.GetString(bytes);
    }

    public static string? TryGetQueryParameterValue(this string url, string parameterName)
    {
        var uri = new Uri(url);
        var queryParameters = HttpUtility.ParseQueryString(uri.Query);
        return queryParameters[parameterName];
    }
}
