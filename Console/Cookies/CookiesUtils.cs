using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace Console.Cookies;

public static class CookiesUtils
{
    private static List<Cookie> ConvertToCookies(List<NetscapeCookie> netscapeCookies)
    {
        var cookies = new List<Cookie>();

        foreach (var nc in netscapeCookies)
        {
            var cookie = new Cookie
            {
                Name = nc.Name,
                Value = nc.Value,
                Domain = nc.Domain,
                Path = nc.Path,
                Secure = nc.IsSecure,
                Expires = DateTimeOffset.FromUnixTimeSeconds(nc.Expiration).DateTime
            };

            cookies.Add(cookie);
        }

        return cookies;
    }

    private static List<Cookie> ParseCookies(string filePath)
    {
        var cookies = new List<NetscapeCookie>();

        foreach (var line in File.ReadLines(filePath))
        {
            // Skip comments and empty lines
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                continue;

            // Split the line into parts
            var parts = line.Split('\t');
            if (parts.Length != 7)
                continue; // Invalid format, skip this line

            var cookie = new NetscapeCookie(
                parts[0],
                parts[1] == "TRUE",
                parts[2],
                parts[3] == "TRUE",
                long.Parse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture),
                parts[5],
                parts[6]
            );

            cookies.Add(cookie);
        }

        return ConvertToCookies(cookies);
    }

    public static List<Cookie> GetCookies(string? path = null)
    {
        if (path is null)
            return [];
        return ParseCookies(path);
    }
}
