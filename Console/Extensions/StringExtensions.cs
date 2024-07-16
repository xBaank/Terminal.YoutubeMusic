using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Console.Extensions;

internal static class StringExtensions
{
    // Temp fix
    // ref https://github.com/gui-cs/Terminal.Gui/issues/2616
    public static string Sanitize(this object obj)
    {
        var str = obj.ToString();

        if (str == null)
            return "";

        var normalizedString = str.Normalize(NormalizationForm.FormD);
        var pattern = @"\p{M}|[\uD800-\uDBFF][\uDC00-\uDFFF]";
        var output = Regex.Replace(normalizedString, pattern, string.Empty);
        return output.Normalize(NormalizationForm.FormC);
    }

    public static string? TryGetQueryParameterValue(this string url, string parameterName)
    {
        var uri = new Uri(url);
        var queryParameters = HttpUtility.ParseQueryString(uri.Query);
        return queryParameters[parameterName];
    }
}
