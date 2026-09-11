using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickScan.Application.Contracts;

namespace QuickScan.Infrastructure.Services;

/// <summary>
/// Detects storage drives on the current system using <see cref="DriveInfo"/>.
/// </summary>
public sealed class WindowsDriveService : IDriveService
{
    /// <inheritdoc/>
    public IReadOnlyList<DriveItem> GetAvailableDrives()
    {
        var drives = new List<DriveItem>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                            ? drive.Name
                            : $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";

                        drives.Add(new DriveItem(
                            Name: label,
                            RootPath: drive.RootDirectory.FullName,
                            TotalSizeBytes: drive.TotalSize,
                            FreeSizeBytes: drive.AvailableFreeSpace,
                            Format: drive.DriveFormat));
                    }
                    else
                    {
                        drives.Add(new DriveItem(
                            Name: drive.Name,
                            RootPath: drive.Name,
                            TotalSizeBytes: 0,
                            FreeSizeBytes: 0,
                            Format: drive.DriveType.ToString()));
                    }
                }
                catch
                {
                    // Drive not ready or permission denied; fallback
                    drives.Add(new DriveItem(
                        Name: drive.Name,
                        RootPath: drive.Name,
                        TotalSizeBytes: 0,
                        FreeSizeBytes: 0,
                        Format: string.Empty));
                }
            }
        }
        catch
        {
            // If DriveInfo.GetDrives() fails, provide root directory as fallback
            drives.Add(new DriveItem(
                Name: "Root",
                RootPath: Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\",
                TotalSizeBytes: 0,
                FreeSizeBytes: 0,
                Format: string.Empty));
        }

        return drives.OrderBy(d => d.RootPath).ToList();
    }
}