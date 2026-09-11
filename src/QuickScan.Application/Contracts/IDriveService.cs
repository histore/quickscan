using System.Collections.Generic;

namespace QuickScan.Application.Contracts;

/// <summary>
/// Contract for enumerating available drives or filesystem roots.
/// </summary>
public interface IDriveService
{
    /// <summary>
    /// Gets all accessible storage drives on the current system.
    /// </summary>
    /// <returns>A read-only collection of detected drives.</returns>
    IReadOnlyList<DriveItem> GetAvailableDrives();
}