namespace QuickScan.Application.Contracts;

/// <summary>
/// Represents a storage drive detected on the host system.
/// </summary>
/// <param name="Name">Display name or volume label of the drive.</param>
/// <param name="RootPath">Root directory path (e.g. 'C:\' or '/').</param>
/// <param name="TotalSizeBytes">Total storage capacity in bytes.</param>
/// <param name="FreeSizeBytes">Available free space in bytes.</param>
/// <param name="Format">File system format if available (e.g. NTFS, ext4).</param>
public sealed record DriveItem(
    string Name,
    string RootPath,
    long TotalSizeBytes,
    long FreeSizeBytes,
    string Format);