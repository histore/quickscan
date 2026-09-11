using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using QuickScan.UI.ViewModels;

namespace QuickScan.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnMainWindowLoaded;
        KeyDown += OnMainWindowKeyDown;
    }

    private void OnMainWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RequestFolderPicker = PickFolderAsync;
            // Scan only starts after an explicit user action (selecting a drive, picking a folder, or scanning from tree)
        }
    }

    private async Task<string?> PickFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Drive or Folder to Scan",
            AllowMultiple = false
        }).ConfigureAwait(true);

        if (folders.Count > 0)
        {
            return folders[0].Path.LocalPath;
        }

        return null;
    }

    private void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        // F9: Toggle Sidebar
        if (e.Key == Key.F9)
        {
            vm.ToggleSidebar();
            e.Handled = true;
        }
        // Alt + Up: Go Up
        else if (e.Key == Key.Up && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            if (vm.CanGoUp)
            {
                _ = vm.GoUpAsync();
                e.Handled = true;
            }
        }
        // Enter: Scan selected node if present
        else if (e.Key == Key.Enter && vm.SelectedNode is not null && !string.IsNullOrWhiteSpace(vm.SelectedNode.Path))
        {
            _ = vm.StartScanAsync(vm.SelectedNode.Path);
            e.Handled = true;
        }
    }

    private void OnBarItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm && BarListBox.SelectedItem is BarItemViewModel item)
        {
            _ = vm.NavigateToItemAsync(item);
        }
    }

    private void OnTreeNodeTapped(object? sender, TappedEventArgs e)
    {
        // Single tap on folder or drive name initiates scan
        if (sender is Control control && control.DataContext is ExplorerNodeViewModel node && DataContext is MainViewModel vm)
        {
            _ = vm.StartScanAsync(node.Path);
            e.Handled = true;
        }
    }
}