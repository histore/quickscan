using System;
using System.Diagnostics;
using System.IO;
using QuickScan.Application.Contracts;

namespace QuickScan.Infrastructure.Services;

/// <summary>
/// Launches files or directory views in the native operating system file manager.
/// </summary>
public sealed class ProcessFileSystemLauncher : IFileSystemLauncher
{
    /// <inheritdoc/>
    public void OpenInFileManager(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (File.Exists(path))
                {
                    // Select file in Explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                }
                else if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", $"\"{path}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", $"\"{path}\"");
            }
        }
        catch
        {
            // Silently handle if system fails to spawn process
        }
    }

    /// <inheritdoc/>
    public void OpenFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently handle if opening fails
        }
    }
}