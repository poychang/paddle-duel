using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PaddleDuel.App;

internal static class AppStrings
{
    private static readonly ResourceLoader Loader = new();

    public static string Get(string key)
    {
        var value = Loader.GetString(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing application string resource: {key}");
        }

        return value;
    }

    public static string Format(string key, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
    }
}
