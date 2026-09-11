using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickScan.Application.Contracts;
using QuickScan.Application.Services;
using QuickScan.Domain.Common;
using QuickScan.Domain.Models;

namespace QuickScan.UI.ViewModels;

/// <summary>
/// Main application ViewModel coordinating user interactions, scans, navigation, and visual presentation.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ScanService _scanService;
    private readonly IDriveService _driveService;
    private readonly IFileSystemLauncher _launcher;
    private readonly IScanCache _cache;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentPath))]
    [NotifyPropertyChangedFor(nameof(CurrentDisplayName))]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInitialPrompt))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyFolderMessage))]
    [NotifyPropertyChangedFor(nameof(IsFileDetailsVisible))]
    [NotifyPropertyChangedFor(nameof(IsChartVisible))]
    [NotifyPropertyChangedFor(nameof(FormattedCreatedAt))]
    [NotifyPropertyChangedFor(nameof(FormattedModifiedAt))]
    [NotifyPropertyChangedFor(nameof(CurrentRootIcon))]
    [NotifyPropertyChangedFor(nameof(CurrentDisplayName))]
    [NotifyPropertyChangedFor(nameof(FormattedSizeInParentheses))]
    private FsEntry? _currentRoot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInitialPrompt))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyFolderMessage))]
    private bool _isScanning;

    public bool ShowInitialPrompt => CurrentRoot is null && !IsScanning;
    public bool ShowEmptyFolderMessage => CurrentRoot is not null && CurrentRoot.IsDirectory && BarItems.Count == 0 && !IsScanning;
    public bool IsFileDetailsVisible => CurrentRoot is not null && !CurrentRoot.IsDirectory;
    public bool IsChartVisible => CurrentRoot is not null && CurrentRoot.IsDirectory;
    public string FormattedCreatedAt => CurrentRoot?.CreatedAt?.LocalDateTime.ToString("G") ?? "-";
    public string FormattedModifiedAt => CurrentRoot?.ModifiedAt?.LocalDateTime.ToString("G") ?? "-";
    public bool HasCurrentPath => !string.IsNullOrWhiteSpace(CurrentPath);
    public string CurrentRootIcon => CurrentRoot is null || CurrentRoot.IsDirectory ? "📁" : "📄";
    public string CurrentDisplayName => CurrentRoot is not null
        ? CurrentRoot.Name
        : (!string.IsNullOrWhiteSpace(CurrentPath)
            ? (Path.GetFileName(CurrentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) is { Length: > 0 } name ? name : CurrentPath)
            : string.Empty);
    public string FormattedSizeInParentheses => !string.IsNullOrWhiteSpace(FormattedTotalSize) && FormattedTotalSize != "0 B"
        ? $"({FormattedTotalSize})"
        : (CurrentRoot is not null ? $"({FormattedTotalSize})" : string.Empty);

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _currentScanningPath = string.Empty;

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private bool _isAboutDialogOpen;

    [ObservableProperty]
    private SortBy _currentSortBy = SortBy.Size;

    [ObservableProperty]
    private SortOrder _currentSortOrder = SortOrder.Descending;

    [ObservableProperty]
    private bool _canGoUp;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSizeInParentheses))]
    private string _formattedTotalSize = "0 B";

    [ObservableProperty]
    private long _totalFiles;

    [ObservableProperty]
    private long _totalFolders;

    [ObservableProperty]
    private DriveItem? _selectedDrive;

    [ObservableProperty]
    private ExplorerNodeViewModel? _selectedNode;

    [ObservableProperty]
    private bool _isSynchronizingSelection;

    public ObservableCollection<BarItemViewModel> BarItems { get; } = new();
    public ObservableCollection<DriveItem> Drives { get; } = new();
    public ObservableCollection<ExplorerNodeViewModel> ExplorerRoots { get; } = new();

    /// <summary>
    /// Gets the application version formatted as vMajor.Minor.Build (e.g. v0.1.0).
    /// </summary>
    public string AppVersion { get; } = GetAppVersion();

    private static string GetAppVersion()
    {
        var version = typeof(MainViewModel).Assembly.GetName().Version;
        if (version is not null)
        {
            return $"v{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
        }
        return "v0.1.0";
    }

    /// <summary>
    /// Action injected by the View to request an interactive folder selection dialog.
    /// </summary>
    public Func<Task<string?>>? RequestFolderPicker { get; set; }

    /// <summary>
    /// Parameterless constructor for XAML designer preview.
    /// </summary>
    public MainViewModel() : this(
        new ScanService(new Infrastructure.Scanning.FastParallelScanner(), new Infrastructure.Services.InMemoryScanCache()),
        new Infrastructure.Services.WindowsDriveService(),
        new Infrastructure.Services.ProcessFileSystemLauncher(),
        new Infrastructure.Services.InMemoryScanCache(),
        new Services.LocalizationService())
    {
    }

    public MainViewModel(
        ScanService scanService,
        IDriveService driveService,
        IFileSystemLauncher launcher,
        IScanCache cache,
        ILocalizationService localizationService)
    {
        _scanService = scanService ?? throw new ArgumentNullException(nameof(scanService));
        _driveService = driveService ?? throw new ArgumentNullException(nameof(driveService));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        _scanService.ScanCompleted += OnScanCompleted;
        _scanService.ProgressChanged += OnProgressChanged;
        _scanService.ScanningStateChanged += OnScanningStateChanged;

        StatusText = _localizationService.GetString("Status.Ready");
        LoadDrives();
    }

    private void LoadDrives()
    {
        Drives.Clear();
        ExplorerRoots.Clear();

        foreach (var drive in _driveService.GetAvailableDrives())
        {
            Drives.Add(drive);
            ExplorerRoots.Add(new ExplorerNodeViewModel(drive.RootPath, drive.Name, true, _cache));
        }

        if (Drives.Count > 0)
        {
            SelectedDrive = Drives[0];
        }
    }

    [RelayCommand]
    public async Task SelectFolderAsync()
    {
        if (RequestFolderPicker is not null)
        {
            var selected = await RequestFolderPicker().ConfigureAwait(true);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                await StartScanAsync(selected).ConfigureAwait(true);
            }
        }
    }

    [RelayCommand]
    public async Task StartScanAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        CurrentPath = path;
        UpdateCanGoUp();
        SynchronizeTreeSelection(path);
        await _scanService.StartScanAsync(path, forceScan: false).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (!string.IsNullOrWhiteSpace(CurrentPath))
        {
            await _scanService.StartScanAsync(CurrentPath, forceScan: true).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    public async Task GoUpAsync()
    {
        if (CanGoUp && !IsScanning)
        {
            var parent = Directory.GetParent(CurrentPath);
            if (parent is not null)
            {
                await StartScanAsync(parent.FullName).ConfigureAwait(true);
            }
        }
    }

    [RelayCommand]
    public void CancelScan()
    {
        _scanService.CancelScan();
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    public void ToggleAboutDialog()
    {
        IsAboutDialogOpen = !IsAboutDialogOpen;
    }

    [RelayCommand]
    public async Task NavigateToItemAsync(BarItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.IsDirectory)
        {
            await StartScanAsync(item.Path).ConfigureAwait(true);
        }
        else
        {
            // Focus on file: display file details screen (matches original quickscan architecture)
            CurrentRoot = item.Model;
            CurrentPath = item.Path;
            FormattedTotalSize = item.FormattedSize;
            TotalFiles = 1;
            TotalFolders = 0;
            UpdateCanGoUp();
            UpdateBarItems();

            var parentDir = Directory.GetParent(item.Path)?.FullName;
            if (!string.IsNullOrWhiteSpace(parentDir))
            {
                SynchronizeTreeSelection(parentDir);
            }
        }
    }

    [RelayCommand]
    public void OpenCurrentFile()
    {
        if (CurrentRoot is not null && !CurrentRoot.IsDirectory)
        {
            _launcher.OpenFile(CurrentRoot.Path);
        }
    }

    [RelayCommand]
    public void OpenInExplorer(string? path)
    {
        var target = path ?? CurrentPath;
        if (!string.IsNullOrWhiteSpace(target))
        {
            _launcher.OpenInFileManager(target);
        }
    }

    [RelayCommand]
    public void Sort(string column)
    {
        var targetSortBy = column?.ToLowerInvariant() switch
        {
            "name" => SortBy.Name,
            "items" => SortBy.Items,
            _ => SortBy.Size
        };

        if (CurrentSortBy == targetSortBy)
        {
            // Toggle order
            CurrentSortOrder = CurrentSortOrder == SortOrder.Ascending
                ? SortOrder.Descending
                : SortOrder.Ascending;
        }
        else
        {
            CurrentSortBy = targetSortBy;
            CurrentSortOrder = SortOrder.Descending;
        }

        UpdateBarItems();
    }

    [RelayCommand]
    public void SetLanguage(string lang)
    {
        _localizationService.SetLanguage(lang);
    }

    [RelayCommand]
    public async Task SelectDriveAsync(DriveItem? drive)
    {
        if (drive is not null && !string.IsNullOrWhiteSpace(drive.RootPath))
        {
            SelectedDrive = drive;
            await StartScanAsync(drive.RootPath).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    public async Task ScanSelectedNodeAsync()
    {
        if (SelectedNode is not null && !string.IsNullOrWhiteSpace(SelectedNode.Path))
        {
            await StartScanAsync(SelectedNode.Path).ConfigureAwait(true);
        }
    }

    private static void RunOnUIThread(Action action)
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(action);
        }
    }

    private void OnScanningStateChanged(bool scanning)
    {
        RunOnUIThread(() =>
        {
            IsScanning = scanning;
            if (!scanning)
            {
                CurrentScanningPath = string.Empty;
                StatusText = _localizationService.GetString("Status.Ready");
            }
        });
    }

    private void OnProgressChanged(ScanProgress progress)
    {
        RunOnUIThread(() =>
        {
            CurrentScanningPath = progress.CurrentPath;
            StatusText = $"{_localizationService.GetString("Status.Scanning")} {progress.FoldersScanned:N0} folders, {progress.FilesScanned:N0} files ({ByteSizeFormatter.Format(progress.BytesScanned)})";
        });
    }

    private void OnScanCompleted(FsEntry root)
    {
        RunOnUIThread(() =>
        {
            CurrentRoot = root;
            CurrentPath = root.Path;
            FormattedTotalSize = root.FormattedSize;
            TotalFiles = root.FileCount;
            TotalFolders = root.DirectoryCount;
            UpdateCanGoUp();
            UpdateBarItems();
            SynchronizeTreeSelection(root.Path);
        });
    }

    /// <summary>
    /// Synchronizes the explorer tree selection with the specified path, expanding all ancestor nodes.
    /// </summary>
    public void SynchronizeTreeSelection(string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }

        try
        {
            IsSynchronizingSelection = true;
            var normalizedTarget = NormalizePath(targetPath);

            // If the current SelectedNode already matches the target, ensure it stays selected without resetting
            if (SelectedNode is not null && string.Equals(NormalizePath(SelectedNode.Path), normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                SelectedNode.IsSelected = true;
                return;
            }

            // Deselect all existing nodes to guarantee a clean single selection
            ClearTreeSelection(ExplorerRoots);

            foreach (var root in ExplorerRoots)
            {
                var normalizedRoot = NormalizePath(root.Path);
                if (IsSameOrAncestor(normalizedRoot, normalizedTarget))
                {
                    var found = FindAndSelectNode(root, normalizedTarget);
                    if (found is not null)
                    {
                        found.IsSelected = true;
                        SelectedNode = found;

                        // Ensure UI container realization does not overwrite selection
                        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (found is not null)
                                {
                                    found.IsSelected = true;
                                    SelectedNode = found;
                                }
                            }, Avalonia.Threading.DispatcherPriority.Loaded);
                        }
                    }
                    break;
                }
            }
        }
        catch
        {
            // Ignore path normalization or traversal exceptions
        }
        finally
        {
            IsSynchronizingSelection = false;
        }
    }

    private static void ClearTreeSelection(IEnumerable<ExplorerNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsSelected)
            {
                node.IsSelected = false;
            }
            if (node.Children.Count > 0)
            {
                ClearTreeSelection(node.Children);
            }
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsSameOrAncestor(string candidateAncestor, string targetPath)
    {
        var normAncestor = NormalizePath(candidateAncestor);
        var normTarget = NormalizePath(targetPath);

        if (string.Equals(normAncestor, normTarget, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = normAncestor.EndsWith(Path.DirectorySeparatorChar)
            ? normAncestor
            : normAncestor + Path.DirectorySeparatorChar;

        return normTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static ExplorerNodeViewModel? FindAndSelectNode(ExplorerNodeViewModel current, string normalizedTarget)
    {
        var currentNorm = NormalizePath(current.Path);
        if (string.Equals(currentNorm, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        // Expand current ancestor node so its children are loaded
        current.IsExpanded = true;

        foreach (var child in current.Children)
        {
            var childNorm = NormalizePath(child.Path);
            if (IsSameOrAncestor(childNorm, normalizedTarget))
            {
                var matched = FindAndSelectNode(child, normalizedTarget);
                if (matched is not null)
                {
                    return matched;
                }
            }
        }

        return null;
    }

    private void UpdateCanGoUp()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath))
        {
            CanGoUp = false;
            return;
        }

        var parent = Directory.GetParent(CurrentPath);
        CanGoUp = parent is not null;
    }

    private void UpdateBarItems()
    {
        BarItems.Clear();

        if (CurrentRoot is null || !CurrentRoot.IsDirectory || CurrentRoot.Children is null || CurrentRoot.Children.Count == 0)
        {
            OnPropertyChanged(nameof(ShowEmptyFolderMessage));
            return;
        }

        var sorted = CurrentRoot.GetSortedChildren(CurrentSortBy, CurrentSortOrder);
        var maxSize = sorted.Count > 0 ? sorted.Max(s => s.Size) : 1;

        foreach (var child in sorted)
        {
            BarItems.Add(new BarItemViewModel(child, CurrentRoot.Size, maxSize));
        }

        OnPropertyChanged(nameof(ShowEmptyFolderMessage));
    }
}