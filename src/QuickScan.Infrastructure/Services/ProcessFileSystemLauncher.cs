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
                    var safePath = path.Replace("\"", "\\\"");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{safePath}\"",
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
                Process.Start(new ProcessStartInfo("open") { ArgumentList = { path }, UseShellExecute = false });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { path }, UseShellExecute = false });
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