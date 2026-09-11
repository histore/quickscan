using QuickScan.Domain.Models;
using QuickScan.Infrastructure.Services;
using Xunit;

namespace QuickScan.Tests.Infrastructure;

public sealed class InMemoryScanCacheTests
{
    [Fact]
    public void SetAndTryGet_WhenEntryCached_ReturnsTrueAndCorrectEntry()
    {
        // Arrange
        var cache = new InMemoryScanCache();
        var entry = new FsEntry("C:\\data", "data", 1024, true, null, null, 1, 0);

        // Act
        cache.Set(entry);
        var found = cache.TryGet("C:\\data", out var result);

        // Assert
        Assert.True(found);
        Assert.NotNull(result);
        Assert.Equal("data", result.Name);
        Assert.Equal(1024, result.Size);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var cache = new InMemoryScanCache();
        var entry = new FsEntry("C:\\data", "data", 1024, true, null, null, 1, 0);
        cache.Set(entry);

        // Act
        cache.Clear();
        var found = cache.TryGet("C:\\data", out _);

        // Assert
        Assert.False(found);
    }
}