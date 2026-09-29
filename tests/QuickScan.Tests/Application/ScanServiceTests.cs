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

    [Fact]
    public async Task StartScanAsync_WhenSupersededByNewScan_PreservesScanningStateUntilLatestCompletes()
    {
        // Arrange
        var tcsFirst = new TaskCompletionSource<FsEntry?>();
        var fakeEngine = new AsyncFakeScanEngine(tcsFirst);
        var cache = new InMemoryScanCache();
        using var service = new ScanService(fakeEngine, cache);

        var stateChanges = new List<bool>();
        service.ScanningStateChanged += state => stateChanges.Add(state);

        // Act 1: Start slow Scan A
        var scanATask = service.StartScanAsync("C:\\SlowScanA");
        Assert.True(service.IsScanning);

        // Act 2: Quickly trigger Scan B before Scan A completes
        var scanBTask = service.StartScanAsync("C:\\FastScanB");

        // Release/cancel Scan A
        tcsFirst.TrySetCanceled();
        await scanATask;

        // Assert: IsScanning must STILL be true because Scan B is still running!
        Assert.True(service.IsScanning);

        // Complete Scan B
        fakeEngine.CompleteSecond();
        await scanBTask;

        // Now IsScanning should be false
        Assert.False(service.IsScanning);
        Assert.Equal("C:\\FastScanB", service.CurrentPath);
    }

    [Fact]
    public async Task StartScanAsync_WhenNavigatingToCachedPathDuringActiveScan_CancelsRunningScan()
    {
        // Arrange
        var tcs = new TaskCompletionSource<FsEntry?>();
        var fakeEngine = new AsyncFakeScanEngine(tcs);
        var cache = new InMemoryScanCache();
        using var service = new ScanService(fakeEngine, cache);

        var cached = new FsEntry("C:\\Cached", "Cached", 100, true, null, null, 1, 0);
        cache.Set(cached);

        // Start active scan
        var scanTask = service.StartScanAsync("C:\\SlowActive");

        // Act: Navigate to cached path while slow scan is running
        await service.StartScanAsync("C:\\Cached");

        // Assert: Cached entry is immediately set as current root
        Assert.Equal("C:\\Cached", service.CurrentPath);
        Assert.Equal(cached, service.CurrentRoot);

        tcs.TrySetCanceled();
        await scanTask;
    }

    private sealed class AsyncFakeScanEngine : IScanEngine
    {
        private readonly TaskCompletionSource<FsEntry?> _firstTcs;
        private readonly TaskCompletionSource<FsEntry?> _secondTcs = new();
        private int _callCount;

        public AsyncFakeScanEngine(TaskCompletionSource<FsEntry?> firstTcs)
        {
            _firstTcs = firstTcs;
        }

        public void CompleteSecond()
        {
            _secondTcs.TrySetResult(new FsEntry("C:\\FastScanB", "FastScanB", 500, true, null, null, 1, 0));
        }

        public async Task<FsEntry?> ScanDirectoryAsync(string path, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var count = Interlocked.Increment(ref _callCount);
            if (count == 1)
            {
                using (cancellationToken.Register(() => _firstTcs.TrySetCanceled()))
                {
                    return await _firstTcs.Task;
                }
            }

            using (cancellationToken.Register(() => _secondTcs.TrySetCanceled()))
            {
                return await _secondTcs.Task;
            }
        }
    }
}