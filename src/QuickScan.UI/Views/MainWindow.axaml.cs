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
        // Enter: Navigate to selected item in main chart or scan selected node in tree
        else if (e.Key == Key.Enter)
        {
            if (BarListBox.SelectedItem is BarItemViewModel barItem)
            {
                _ = vm.NavigateToItemAsync(barItem);
                e.Handled = true;
            }
            else if (vm.SelectedNode is not null && !string.IsNullOrWhiteSpace(vm.SelectedNode.Path))
            {
                _ = vm.StartScanAsync(vm.SelectedNode.Path);
                e.Handled = true;
            }
        }
    }

    private void OnBarItemTapped(object? sender, TappedEventArgs e)
    {
        // Single tap / click navigates into directory or opens file details
        if (sender is Control control && control.DataContext is BarItemViewModel item && DataContext is MainViewModel vm)
        {
            _ = vm.NavigateToItemAsync(item);
            e.Handled = true;
        }
    }

    private void OnBarItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm && BarListBox.SelectedItem is BarItemViewModel item)
        {
            _ = vm.NavigateToItemAsync(item);
            e.Handled = true;
        }
    }

    private void OnTreeNodeTapped(object? sender, TappedEventArgs e)
    {
        // Single tap on folder or drive name initiates scan and marks node
        if (sender is Control control && control.DataContext is ExplorerNodeViewModel node && DataContext is MainViewModel vm)
        {
            vm.SelectedNode = node;
            node.IsSelected = true;
            _ = vm.StartScanAsync(node.Path);
            e.Handled = true;
        }
    }
}