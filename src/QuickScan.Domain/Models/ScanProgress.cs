namespace QuickScan.Domain.Models;

/// <summary>
/// Represents real-time progress information during a file system scan.
/// </summary>
/// <param name="CurrentPath">The directory path currently being traversed.</param>
/// <param name="FoldersScanned">The total number of folders scanned so far.</param>
/// <param name="FilesScanned">The total number of files discovered so far.</param>
/// <param name="BytesScanned">The cumulative size in bytes calculated so far.</param>
/// <param name="IsCompleted">Indicates whether the scan has completed.</param>
/// <param name="IsCancelled">Indicates whether the scan was cancelled.</param>
public sealed record ScanProgress(
    string CurrentPath,
    long FoldersScanned,
    long FilesScanned,
    long BytesScanned,
    bool IsCompleted = false,
    bool IsCancelled = false);