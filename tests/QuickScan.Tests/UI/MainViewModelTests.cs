using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickScan.Application.Contracts;
using QuickScan.Application.Services;
using QuickScan.Domain.Models;
using QuickScan.Infrastructure.Services;
using QuickScan.UI.ViewModels;
using Xunit;

namespace QuickScan.Tests.UI;

public sealed class MainViewModelTests
{
    private sealed class FakeScanEngine : IScanEngine
    {
        public int ScanCount { get; private set; }

        public Task<FsEntry?> ScanDirectoryAsync(string path, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            ScanCount++;
            var entry = new FsEntry(path, Path.GetFileName(path), 1024, true, null, null, 1, 0);
            return Task.FromResult<FsEntry?>(entry);
        }
    }

    private sealed class FakeDriveService : IDriveService
    {
        public IReadOnlyList<DriveItem> GetAvailableDrives()
        {
            return new List<DriveItem>
            {
                new("System (C:)", "C:\\", 100_000, 50_000, "NTFS"),
                new("Data (D:)", "D:\\", 200_000, 150_000, "NTFS")
            };
        }
    }

    private sealed class FakeFileSystemLauncher : IFileSystemLauncher
    {
        public void OpenFile(string filePath) { }
        public void OpenInFileManager(string folderPath) { }
    }

    private sealed class FakeLocalizationService : ILocalizationService
    {
        public string CurrentLanguage => "en";
        public string GetString(string key) => key == "Status.Ready" ? "Ready" : key;
        public void SetLanguage(string cultureCode) { }
        public event Action? LanguageChanged
        {
            add { }
            remove { }
        }
    }

    [Fact]
    public void Constructor_InitializesInReadyState_WithoutStartingScan()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();

        // Act
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        // Assert: Scan must NOT start automatically on construction
        Assert.False(vm.IsScanning);
        Assert.Equal(0, fakeEngine.ScanCount);
        Assert.Equal("Ready", vm.StatusText);
        Assert.Empty(vm.CurrentPath);
        Assert.Equal(2, vm.Drives.Count);
        Assert.Equal(2, vm.ExplorerRoots.Count);
        Assert.NotNull(vm.SelectedDrive);
        Assert.Null(vm.SelectedNode);
        Assert.True(vm.ShowInitialPrompt);
        Assert.False(vm.ShowEmptyFolderMessage);
    }

    [Fact]
    public async Task SelectDriveAsync_ExplicitlyStartsScanForSelectedDrive()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var targetDrive = vm.Drives[1]; // D:\

        // Act: User explicitly selects drive
        await vm.SelectDriveAsync(targetDrive);

        // Assert: Scan was initiated for chosen drive
        Assert.Equal(1, fakeEngine.ScanCount);
        Assert.Equal(targetDrive.RootPath, vm.CurrentPath);
        Assert.Equal(targetDrive, vm.SelectedDrive);
    }

    [Fact]
    public async Task ScanSelectedNodeAsync_WhenNodeSelected_StartsScanForNode()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var tempDir = Path.GetTempPath().TrimEnd('\\');
        var node = new ExplorerNodeViewModel(tempDir, "Temp", true, cache);
        vm.SelectedNode = node;

        // Act
        await vm.ScanSelectedNodeAsync();

        // Assert
        Assert.Equal(1, fakeEngine.ScanCount);
        Assert.Equal(tempDir, vm.CurrentPath);
    }

    [Fact]
    public async Task NavigateToItemAsync_WhenFileSelected_DisplaysFileDetailsScreen()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var filePath = Path.Combine(Path.GetTempPath(), "testfile.txt");
        var fileEntry = new FsEntry(filePath, "testfile.txt", 4096, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, 0);
        var barItem = new BarItemViewModel(fileEntry, 10000, 10000);

        // Act
        await vm.NavigateToItemAsync(barItem);

        // Assert
        Assert.True(vm.IsFileDetailsVisible);
        Assert.False(vm.IsChartVisible);
        Assert.Equal(filePath, vm.CurrentPath);
        Assert.Equal(fileEntry, vm.CurrentRoot);
        Assert.True(vm.CanGoUp);
    }
}
