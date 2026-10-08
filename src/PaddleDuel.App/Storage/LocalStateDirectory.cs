using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Windows.Storage;

namespace PaddleDuel.App.Storage;

internal static class LocalStateDirectory
{
    private const int ErrorInsufficientBuffer = 122;
    private const int AppModelErrorNoPackage = 15700;

    public static string GetPath()
    {
        var packageFullNameLength = 0;
        var result = GetCurrentPackageFullName(ref packageFullNameLength, null);
        if (result == AppModelErrorNoPackage)
        {
            // Keep the legacy directory for explicitly unpackaged development builds.
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Arcade1972");
        }

        if (result != ErrorInsufficientBuffer)
        {
            throw new Win32Exception(result, "Unable to determine the application package identity.");
        }

        using var applicationData = ApplicationData.GetDefault();
        return applicationData.LocalPath;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(
        ref int packageFullNameLength,
        char[]? packageFullName);
}
