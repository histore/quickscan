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

    [Fact]
    public void HeaderBanner_WhenNoPathSelected_IsHidden()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        // Assert: Before any selection, banner should be hidden
        Assert.False(vm.HasCurrentPath);
        Assert.Equal(string.Empty, vm.CurrentDisplayName);
        Assert.Equal("📁", vm.CurrentRootIcon);
    }

    [Fact]
    public async Task HeaderBanner_WhenFolderSelected_ShowsNameAndFormattedSize()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var folderPath = "C:\\Windows";

        // Act
        await vm.StartScanAsync(folderPath);

        // Assert (REQ-QS-020)
        Assert.True(vm.HasCurrentPath);
        Assert.Equal("Windows", vm.CurrentDisplayName);
        Assert.Equal("📁", vm.CurrentRootIcon);
        Assert.Equal("(1.00 KB)", vm.FormattedSizeInParentheses);
        Assert.Equal("1.00 KB", vm.FormattedTotalSize);
    }

    [Fact]
    public void SynchronizeTreeSelection_ExpandsAncestorsAndSelectsTarget()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        // Setup a deterministic hierarchy in cache for root C:\
        // C:\ -> Projects -> QuickScan
        var quickScanEntry = new FsEntry("C:\\Projects\\QuickScan", "QuickScan", 5000, true, null, null, 2, 0);
        var projectsEntry = new FsEntry("C:\\Projects", "Projects", 10000, true, null, null, 10, 1, new List<FsEntry> { quickScanEntry });
        var rootEntry = new FsEntry("C:\\", "C:\\", 100000, true, null, null, 50, 1, new List<FsEntry> { projectsEntry });

        cache.Set(rootEntry);
        cache.Set(projectsEntry);
        cache.Set(quickScanEntry);

        // Act: Synchronize to target folder
        vm.SynchronizeTreeSelection("C:\\Projects\\QuickScan");

        // Assert (REQ-QS-021)
        Assert.NotNull(vm.SelectedNode);
        Assert.Equal("QuickScan", vm.SelectedNode.Name);
        Assert.True(vm.SelectedNode.IsSelected);

        // Root C:\ and parent Projects must both be expanded
        var rootNode = vm.ExplorerRoots[0];
        Assert.True(rootNode.IsExpanded);

        var projectsNode = rootNode.Children[0];
        Assert.True(projectsNode.IsExpanded);
    }

    [Fact]
    public void SynchronizeTreeSelection_ClearsPreviousSelectionWhenSwitchingFolders()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var folderA = new FsEntry("C:\\FolderA", "FolderA", 1000, true, null, null, 1, 0);
        var folderB = new FsEntry("C:\\FolderB", "FolderB", 2000, true, null, null, 1, 0);
        var rootEntry = new FsEntry("C:\\", "C:\\", 10000, true, null, null, 2, 2, new List<FsEntry> { folderA, folderB });

        cache.Set(rootEntry);
        cache.Set(folderA);
        cache.Set(folderB);

        // Act 1: Select FolderA
        vm.SynchronizeTreeSelection("C:\\FolderA");
        var nodeA = vm.SelectedNode;
        Assert.NotNull(nodeA);
        Assert.True(nodeA.IsSelected);
        Assert.Equal("FolderA", nodeA.Name);

        // Act 2: Switch to FolderB
        vm.SynchronizeTreeSelection("C:\\FolderB");
        var nodeB = vm.SelectedNode;
        Assert.NotNull(nodeB);
        Assert.True(nodeB.IsSelected);
        Assert.Equal("FolderB", nodeB.Name);

        // Assert: FolderA must be deselected, only FolderB selected
        Assert.False(nodeA.IsSelected);
    }

    [Fact]
    public async Task NavigateToItemAsync_WhenDirectoryItem_StartsScanForDirectory()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var dirEntry = new FsEntry("C:\\SubDir", "SubDir", 4096, true, null, null, 5, 1);
        var barItem = new BarItemViewModel(dirEntry, 10000, 10000);

        // Act: Navigate to directory (single click)
        await vm.NavigateToItemAsync(barItem);

        // Assert
        Assert.Equal(1, fakeEngine.ScanCount);
        Assert.Equal("C:\\SubDir", vm.CurrentPath);
    }

    [Fact]
    public async Task NavigateToItemAsync_WhenNavigatingToSubdirectory_SelectsSubdirectoryInTree()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var datenEntry = new FsEntry("C:\\daten", "daten", 5000, true, null, null, 2, 0);
        var rootEntry = new FsEntry("C:\\", "C:\\", 100000, true, null, null, 50, 1, new List<FsEntry> { datenEntry });

        cache.Set(rootEntry);
        cache.Set(datenEntry);

        // First scan root
        await vm.StartScanAsync("C:\\");

        Assert.NotNull(vm.SelectedNode);
        Assert.Equal("C:\\", vm.SelectedNode.Path);
        Assert.True(vm.SelectedNode.IsSelected);

        // Act: User clicks on "daten" in the main list
        var barItem = new BarItemViewModel(datenEntry, 100000, 100000);
        await vm.NavigateToItemAsync(barItem);

        // Assert
        Assert.NotNull(vm.SelectedNode);
        Assert.Equal("C:\\daten", vm.SelectedNode.Path);
        Assert.True(vm.SelectedNode.IsSelected);
    }

    [Fact]
    public void SynchronizeTreeSelection_WhenTargetAlreadySelected_PreservesSelectionWithoutResetting()
    {
        // Arrange
        var fakeEngine = new FakeScanEngine();
        var cache = new InMemoryScanCache();
        using var scanService = new ScanService(fakeEngine, cache);
        var driveService = new FakeDriveService();
        var launcher = new FakeFileSystemLauncher();
        var locService = new FakeLocalizationService();
        var vm = new MainViewModel(scanService, driveService, launcher, cache, locService);

        var subDir = new FsEntry("C:\\Projects", "Projects", 1000, true, null, null, 1, 0);
        var rootEntry = new FsEntry("C:\\", "C:\\", 5000, true, null, null, 2, 1, new List<FsEntry> { subDir });
        cache.Set(rootEntry);
        cache.Set(subDir);

        vm.SynchronizeTreeSelection("C:\\Projects");
        var originalSelectedNode = vm.SelectedNode;
        Assert.NotNull(originalSelectedNode);
        Assert.True(originalSelectedNode.IsSelected);

        // Act: Synchronize again to the exact same path
        vm.SynchronizeTreeSelection("C:\\Projects");

        // Assert: Same instance preserved, still selected, flag reset to false
        Assert.NotNull(vm.SelectedNode);
        Assert.Same(originalSelectedNode, vm.SelectedNode);
        Assert.True(vm.SelectedNode.IsSelected);
        Assert.False(vm.IsSynchronizingSelection);
    }

    [Fact]
    public void AppVersion_MatchesSemVerFormatWithVPrefix()
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

        // Assert: Format must be v0.0.0 (e.g. v0.1.1)
        Assert.NotNull(vm.AppVersion);
        Assert.Matches(@"^v\d+\.\d+\.\d+$", vm.AppVersion);
        Assert.Equal("v0.1.1", vm.AppVersion);
    }
}


