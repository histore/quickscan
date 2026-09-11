using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickScan.Domain.Models;

namespace QuickScan.UI.ViewModels;

/// <summary>
/// ViewModel representing an individual bar item in the disk usage chart.
/// </summary>
public sealed partial class BarItemViewModel : ObservableObject
{
    private static readonly IBrush FolderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 150, 255));
    private static readonly IBrush FileBrush = new SolidColorBrush(Color.FromArgb(200, 0, 230, 118));
    private static readonly IBrush FolderBorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 150, 255));
    private static readonly IBrush FileBorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 230, 118));

    public FsEntry Model { get; }

    public string Path => Model.Path;
    public string Name => Model.Name;
    public long Size => Model.Size;
    public string FormattedSize => Model.FormattedSize;
    public bool IsDirectory => Model.IsDirectory;
    public long FileCount => Model.FileCount;
    public long DirectoryCount => Model.DirectoryCount;

    public string Icon => IsDirectory ? "📁" : "📄";

    public double Percentage { get; }
    public string FormattedPercentage => $"{Percentage * 100:F1} %";

    /// <summary>
    /// Relative width percentage for the visual bar (0 to 100).
    /// </summary>
    public double BarWidthPercentage { get; }

    public IBrush BarBrush => IsDirectory ? FolderBrush : FileBrush;
    public IBrush BorderBrush => IsDirectory ? FolderBorderBrush : FileBorderBrush;

    public string TooltipDetails => IsDirectory
        ? $"{Name}\nSize: {FormattedSize}\nFolders: {DirectoryCount:N0}\nFiles: {FileCount:N0}"
        : $"{Name}\nSize: {FormattedSize}";

    public BarItemViewModel(FsEntry model, long parentSize, long maxSizeInView)
    {
        Model = model;

        Percentage = parentSize > 0
            ? Math.Clamp((double)model.Size / parentSize, 0.0, 1.0)
            : 0.0;

        BarWidthPercentage = maxSizeInView > 0
            ? Math.Max(1.0, Math.Clamp((double)model.Size / maxSizeInView * 100.0, 0.0, 100.0))
            : 0.0;
    }
}