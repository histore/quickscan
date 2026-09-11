using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickScan.Application.Contracts;
using QuickScan.Application.Services;
using QuickScan.Domain.Models;
using QuickScan.Infrastructure.Services;
using Xunit;

namespace QuickScan.Tests.Application;

public sealed class ScanServiceTests
{
    private sealed class FakeScanEngine : IScanEngine
    {
        public int ScanCount { get; private set; }

        public Task<FsEntry?> ScanDirectoryAsync(string path, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            ScanCount++;
            var entry = new FsEntry(path, Path.GetFileName(path), 2048, true, null, null, 5, 1);
            return Task.FromResult<FsEntry?>(entry);
        }
    }

    [Fact]
    public async Task StartScanAsync_CompletesSuccessfully_UpdatesCurrentRootAndInvokesEvent()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var service = new ScanService(fakeEngine, cache);

        FsEntry? eventResult = null;
        service.ScanCompleted += entry => eventResult = entry;

        var testPath = Path.GetTempPath().TrimEnd('\\');

        // Act
        await service.StartScanAsync(testPath);

        // Assert
        Assert.NotNull(service.CurrentRoot);
        Assert.NotNull(eventResult);
        Assert.Equal(service.CurrentRoot, eventResult);
        Assert.Equal(1, fakeEngine.ScanCount);
    }

    [Fact]
    public async Task StartScanAsync_WhenPathCachedAndNotForced_UsesCacheWithoutCallingScanEngine()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var service = new ScanService(fakeEngine, cache);

        var testPath = Path.GetTempPath().TrimEnd('\\');
        await service.StartScanAsync(testPath);
        Assert.Equal(1, fakeEngine.ScanCount);

        // Act: scan again with forceScan = false
        await service.StartScanAsync(testPath, forceScan: false);

        // Assert
        Assert.Equal(1, fakeEngine.ScanCount); // Still 1, served from cache!
    }

    [Fact]
    public async Task GoUpAsync_WhenRootHasParent_NavigatesToParent()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var service = new ScanService(fakeEngine, cache);

        var childPath = Path.Combine(Path.GetTempPath(), "subdir");
        await service.StartScanAsync(childPath);

        // Act
        var wentUp = await service.GoUpAsync();

        // Assert
        Assert.True(wentUp);
        Assert.Equal(2, fakeEngine.ScanCount);
    }
}