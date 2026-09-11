namespace QuickScan.Domain.Models;

/// <summary>
/// Specifies the field by which file system entries are sorted.
/// </summary>
public enum SortBy
{
    /// <summary>
    /// Sort by file or directory name.
    /// </summary>
    Name,

    /// <summary>
    /// Sort by total byte size.
    /// </summary>
    Size,

    /// <summary>
    /// Sort by count of contained items.
    /// </summary>
    Items
}