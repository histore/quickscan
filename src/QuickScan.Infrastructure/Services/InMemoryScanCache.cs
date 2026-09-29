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

        if (_cache.TryGetValue(path, out entry))
        {
            return true;
        }

        var normalized = NormalizeKey(path);
        return _cache.TryGetValue(normalized, out entry);
    }

    /// <inheritdoc/>
    public void Set(FsEntry rootEntry)
    {
        ArgumentNullException.ThrowIfNull(rootEntry);
        rootEntry.FillCache(_cache);
        var norm = NormalizeKey(rootEntry.Path);
        if (!string.IsNullOrEmpty(norm))
        {
            _cache[norm] = rootEntry;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return _cache.ContainsKey(path) || _cache.ContainsKey(NormalizeKey(path));
    }

    private static string NormalizeKey(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var trimmed = path.Trim();
            if (trimmed.Length == 2 && trimmed[1] == ':')
            {
                trimmed += System.IO.Path.DirectorySeparatorChar;
            }

            var fullPath = System.IO.Path.GetFullPath(trimmed);
            var root = System.IO.Path.GetPathRoot(fullPath);
            if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            }

            return fullPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }
    }
}