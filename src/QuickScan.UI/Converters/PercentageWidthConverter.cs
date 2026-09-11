using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QuickScan.UI.Converters;

/// <summary>
/// Multi-value converter that calculates a proportional pixel width from total container width and a percentage (0..100).
/// </summary>
public sealed class PercentageWidthConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 &&
            values[0] is double totalWidth &&
            values[1] is double percentage &&
            !double.IsNaN(totalWidth) &&
            totalWidth > 0)
        {
            var calculated = totalWidth * Math.Clamp(percentage / 100.0, 0.0, 1.0);
            return Math.Max(2.0, calculated);
        }

        return 0.0;
    }
}