using System;
using System.Collections.Concurrent;
using QuickScan.Application.Contracts;
using QuickScan.Domain.Models;

namespace QuickScan.Infrastructure.Services;

/// <summary>
/// Thread-safe in-memory cache for scanned file system trees.
/// </summary>
public sealed class InMemoryScanCache : IScanCache
{
    private readonly ConcurrentDictionary<string, FsEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool TryGet(string path, out FsEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            entry = null;
            return false;
        }

        return _cache.TryGetValue(path, out entry);
    }

    /// <inheritdoc/>
    public void Set(FsEntry rootEntry)
    {
        ArgumentNullException.ThrowIfNull(rootEntry);
        rootEntry.FillCache(_cache);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && _cache.ContainsKey(path);
    }
}