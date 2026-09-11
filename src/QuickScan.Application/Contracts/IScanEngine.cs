using System;
using System.Threading;
using System.Threading.Tasks;
using QuickScan.Domain.Models;

namespace QuickScan.Application.Contracts;

/// <summary>
/// Contract for scanning file system directories.
/// </summary>
public interface IScanEngine
{
    /// <summary>
    /// Scans a directory recursively and aggregates statistics for all subfolders and files.
    /// </summary>
    /// <param name="path">The root directory path to scan.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token to abort the scan.</param>
    /// <returns>The aggregated <see cref="FsEntry"/> tree, or null if cancelled.</returns>
    Task<FsEntry?> ScanDirectoryAsync(
        string path,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}