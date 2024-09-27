namespace Console.Cookies;

public record NetscapeCookie(
    string Domain,
    bool IsSecure,
    string Path,
    bool IsHttpOnly,
    long Expiration,
    string Name,
    string Value
);
