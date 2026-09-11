using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickScan.Domain.Common;

namespace QuickScan.Domain.Models;

/// <summary>
/// Represents a file system node (file or directory) with recursive size and count statistics.
/// </summary>
public sealed class FsEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FsEntry"/> class.
    /// </summary>
    public FsEntry(
        string path,
        string name,
        long size,
        bool isDirectory,
        DateTimeOffset? createdAt,
        DateTimeOffset? modifiedAt,
        long fileCount,
        long directoryCount,
        IReadOnlyList<FsEntry>? children = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Name = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) ?? path : name;
        Size = size;
        IsDirectory = isDirectory;
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        FileCount = fileCount;
        DirectoryCount = directoryCount;
        Children = children;
    }

    /// <summary>
    /// Gets the absolute path of this entry.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the display name of this entry.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the cumulative size in bytes.
    /// </summary>
    public long Size { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this entry represents a directory.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// Gets the creation timestamp if available.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>
    /// Gets the last modification timestamp if available.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; }

    /// <summary>
    /// Gets the total number of files contained in this subtree (1 for a file).
    /// </summary>
    public long FileCount { get; }

    /// <summary>
    /// Gets the total number of subdirectories contained in this subtree (0 for a file).
    /// </summary>
    public long DirectoryCount { get; }

    /// <summary>
    /// Gets the child entries if this is a directory; otherwise null.
    /// </summary>
    public IReadOnlyList<FsEntry>? Children { get; private set; }

    /// <summary>
    /// Gets the human-readable formatted size string.
    /// </summary>
    public string FormattedSize => ByteSizeFormatter.Format(Size);

    /// <summary>
    /// Finds an entry in the tree matching the specified absolute path.
    /// </summary>
    /// <param name="targetPath">The path to search for.</param>
    /// <returns>The matching <see cref="FsEntry"/> if found; otherwise null.</returns>
    public FsEntry? FindEntry(string targetPath)
    {
        if (string.Equals(Path, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        if (Children is null)
        {
            return null;
        }

        // Only search inside children if targetPath starts with this path
        if (targetPath.StartsWith(Path, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var child in Children)
            {
                var found = child.FindEntry(targetPath);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether the tree contains an entry with the specified path.
    /// </summary>
    /// <param name="targetPath">The path to check.</param>
    /// <returns>True if the path exists within this subtree; otherwise false.</returns>
    public bool ContainsPath(string targetPath)
    {
        return FindEntry(targetPath) is not null;
    }

    /// <summary>
    /// Recursively populates a dictionary cache mapping paths to entries.
    /// </summary>
    /// <param name="cache">The dictionary to populate.</param>
    public void FillCache(IDictionary<string, FsEntry> cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        cache[Path] = this;

        if (Children is not null)
        {
            foreach (var child in Children)
            {
                child.FillCache(cache);
            }
        }
    }

    /// <summary>
    /// Returns sorted children according to the specified criteria and order.
    /// </summary>
    /// <param name="sortBy">The sorting attribute.</param>
    /// <param name="sortOrder">The sort direction.</param>
    /// <returns>An ordered collection of child entries.</returns>
    public IReadOnlyList<FsEntry> GetSortedChildren(SortBy sortBy, SortOrder sortOrder)
    {
        if (Children is null || Children.Count == 0)
        {
            return Array.Empty<FsEntry>();
        }

        IEnumerable<FsEntry> query = sortBy switch
        {
            SortBy.Name => sortOrder == SortOrder.Ascending
                ? Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                : Children.OrderBy(c => !c.IsDirectory).ThenByDescending(c => c.Name, StringComparer.CurrentCultureIgnoreCase),

            SortBy.Size => sortOrder == SortOrder.Ascending
                ? Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Size)
                : Children.OrderBy(c => !c.IsDirectory).ThenByDescending(c => c.Size),

            SortBy.Items => sortOrder == SortOrder.Ascending
                ? Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.FileCount + c.DirectoryCount)
                : Children.OrderBy(c => !c.IsDirectory).ThenByDescending(c => c.FileCount + c.DirectoryCount),

            _ => Children
        };

        return query.ToList();
    }
}