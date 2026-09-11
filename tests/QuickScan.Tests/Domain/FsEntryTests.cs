using System;
using System.Collections.Generic;
using QuickScan.Domain.Models;
using Xunit;

namespace QuickScan.Tests.Domain;

public sealed class FsEntryTests
{
    [Fact]
    public void FindEntry_WhenChildExists_ReturnsMatchingChild()
    {
        // Arrange
        var subChild = new FsEntry("C:\\test\\sub\\file.txt", "file.txt", 100, false, null, null, 1, 0);
        var subDir = new FsEntry("C:\\test\\sub", "sub", 100, true, null, null, 1, 0, new[] { subChild });
        var root = new FsEntry("C:\\test", "test", 100, true, null, null, 1, 1, new[] { subDir });

        // Act
        var found = root.FindEntry("C:\\test\\sub\\file.txt");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("file.txt", found.Name);
        Assert.Equal(100, found.Size);
    }

    [Fact]
    public void ContainsPath_WhenPathDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var root = new FsEntry("C:\\test", "test", 0, true, null, null, 0, 0, Array.Empty<FsEntry>());

        // Act
        var exists = root.ContainsPath("C:\\test\\nonexistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void GetSortedChildren_SortedBySizeDescending_ReturnsLargestFirst()
    {
        // Arrange
        var fileSmall = new FsEntry("C:\\test\\small.bin", "small.bin", 50, false, null, null, 1, 0);
        var fileBig = new FsEntry("C:\\test\\big.bin", "big.bin", 5000, false, null, null, 1, 0);
        var dirMedium = new FsEntry("C:\\test\\mediumDir", "mediumDir", 1000, true, null, null, 2, 0);

        var root = new FsEntry("C:\\test", "test", 6050, true, null, null, 4, 1, new[] { fileSmall, fileBig, dirMedium });

        // Act
        var sorted = root.GetSortedChildren(SortBy.Size, SortOrder.Descending);

        // Assert
        Assert.Equal(3, sorted.Count);
        Assert.Equal("mediumDir", sorted[0].Name); // Directories grouped first
        Assert.Equal("big.bin", sorted[1].Name);
        Assert.Equal("small.bin", sorted[2].Name);
    }

    [Fact]
    public void FillCache_PopulatesAllDescendantsInDictionary()
    {
        // Arrange
        var file1 = new FsEntry("C:\\root\\a.txt", "a.txt", 10, false, null, null, 1, 0);
        var file2 = new FsEntry("C:\\root\\b.txt", "b.txt", 20, false, null, null, 1, 0);
        var root = new FsEntry("C:\\root", "root", 30, true, null, null, 2, 0, new[] { file1, file2 });
        var cache = new Dictionary<string, FsEntry>(StringComparer.OrdinalIgnoreCase);

        // Act
        root.FillCache(cache);

        // Assert
        Assert.Equal(3, cache.Count);
        Assert.True(cache.ContainsKey("C:\\root"));
        Assert.True(cache.ContainsKey("C:\\root\\a.txt"));
        Assert.True(cache.ContainsKey("C:\\root\\b.txt"));
    }
}