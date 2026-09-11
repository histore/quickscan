using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickScan.Application.Contracts;
using QuickScan.Domain.Models;

namespace QuickScan.UI.ViewModels;

/// <summary>
/// ViewModel representing a node in the directory tree sidebar.
/// </summary>
public sealed partial class ExplorerNodeViewModel : ObservableObject
{
    private readonly IScanCache _cache;
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isLoaded;

    public string Path { get; }
    public string Name { get; }
    public bool IsDirectory { get; }

    public ObservableCollection<ExplorerNodeViewModel> Children { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value) && value)
            {
                EnsureChildrenLoaded();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Icon => IsDirectory ? "📁" : "📄";

    public ExplorerNodeViewModel(string path, string name, bool isDirectory, IScanCache cache)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Name = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) ?? path : name;
        IsDirectory = isDirectory;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        if (IsDirectory)
        {
            // Add a dummy child for expandable placeholder
            Children.Add(new ExplorerNodeViewModel(System.IO.Path.Combine(path, "__dummy__"), string.Empty, false, cache));
        }
    }

    private void EnsureChildrenLoaded()
    {
        if (_isLoaded || !IsDirectory)
        {
            return;
        }

        _isLoaded = true;
        Children.Clear();

        // Check cache first
        if (_cache.TryGet(Path, out var cached) && cached?.Children is not null)
        {
            foreach (var child in cached.Children.Where(c => c.IsDirectory))
            {
                Children.Add(new ExplorerNodeViewModel(child.Path, child.Name, true, _cache));
            }
            return;
        }

        // Live filesystem directory enumeration fallback
        try
        {
            var dirInfo = new DirectoryInfo(Path);
            foreach (var subDir in dirInfo.EnumerateDirectories().OrderBy(d => d.Name))
            {
                if ((subDir.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                Children.Add(new ExplorerNodeViewModel(subDir.FullName, subDir.Name, true, _cache));
            }
        }
        catch
        {
            // Access denied or not readable
        }
    }
}