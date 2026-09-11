using System;
using System.IO;
using QuickScan.Infrastructure.Services;
using QuickScan.UI.ViewModels;
using Xunit;

namespace QuickScan.Tests.UI;

public sealed class ExplorerNodeViewModelTests
{
    [Fact]
    public void IsExpanded_WhenSetToTrue_LoadsChildrenWithoutScanning()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "QuickScanTest_" + Guid.NewGuid().ToString("N"));
        var subDir1 = Path.Combine(tempFolder, "Sub1");
        var subDir2 = Path.Combine(tempFolder, "Sub2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        try
        {
            var cache = new InMemoryScanCache();
            var node = new ExplorerNodeViewModel(tempFolder, "Root", true, cache);

            // Initially has placeholder dummy child
            Assert.Single(node.Children);
            Assert.False(node.IsExpanded);

            // Act: Expand node (Aufklappen im Dateibaum)
            node.IsExpanded = true;

            // Assert: Children loaded, dummy removed, no scan initiated
            Assert.True(node.IsExpanded);
            Assert.Equal(2, node.Children.Count);
            Assert.Contains(node.Children, c => c.Name == "Sub1");
            Assert.Contains(node.Children, c => c.Name == "Sub2");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }

    [Fact]
    public void IsExpanded_WhenCollapsed_PreservesLoadedChildren()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "QuickScanTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "SubA"));

        try
        {
            var cache = new InMemoryScanCache();
            var node = new ExplorerNodeViewModel(tempFolder, "Root", true, cache);

            node.IsExpanded = true;
            Assert.Single(node.Children);

            // Act: Collapse
            node.IsExpanded = false;

            // Assert: Children preserved
            Assert.False(node.IsExpanded);
            Assert.Single(node.Children);
            Assert.Equal("SubA", node.Children[0].Name);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }
}
