using QuickScan.Domain.Models;

namespace QuickScan.Application.Contracts;

/// <summary>
/// Contract for in-memory caching of scanned directory trees.
/// </summary>
public interface IScanCache
{
    /// <summary>
    /// Attempts to retrieve a cached entry for the given path.
    /// </summary>
    /// <param name="path">The path to look up.</param>
    /// <param name="entry">The found entry, if available.</param>
    /// <returns>True if the path exists in the cache; otherwise false.</returns>
    bool TryGet(string path, out FsEntry? entry);

    /// <summary>
    /// Caches the entire entry hierarchy.
    /// </summary>
    /// <param name="rootEntry">The root entry of the scanned tree.</param>
    void Set(FsEntry rootEntry);

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    void Clear();
}