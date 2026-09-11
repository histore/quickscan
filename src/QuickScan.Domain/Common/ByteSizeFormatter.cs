using System;
using System.Globalization;

namespace QuickScan.Domain.Common;

/// <summary>
/// Utility for formatting raw byte counts into human-readable strings.
/// </summary>
public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <summary>
    /// Formats a byte size into a compact human-readable string with units.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <param name="provider">Optional format provider. Defaults to invariant culture.</param>
    /// <returns>A formatted string such as '12.45 MB' or '0 B'.</returns>
    public static string Format(long bytes, IFormatProvider? provider = null)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        var formatProvider = provider ?? CultureInfo.InvariantCulture;
        int unitIndex = 0;
        double size = bytes;

        while (size >= 1024.0 && unitIndex < Units.Length - 1)
        {
            size /= 1024.0;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} B"
            : $"{size.ToString("F2", formatProvider)} {Units[unitIndex]}";
    }
}