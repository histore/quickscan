using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickScan.Infrastructure.Scanning;
using Xunit;

namespace QuickScan.Tests.Infrastructure;

public sealed class FastParallelScannerTests : IDisposable
{
    private readonly string _testDir;

    public FastParallelScannerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "QuickScanTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task ScanDirectoryAsync_WithFilesAndSubfolders_ComputesAccurateStatistics()
    {
        // Arrange
        var subDir = Path.Combine(_testDir, "sub");
        Directory.CreateDirectory(subDir);

        File.WriteAllBytes(Path.Combine(_testDir, "file1.bin"), new byte[100]);
        File.WriteAllBytes(Path.Combine(_testDir, "file2.bin"), new byte[200]);
        File.WriteAllBytes(Path.Combine(subDir, "subfile.bin"), new byte[300]);

        var scanner = new FastParallelScanner();

        // Act
        var result = await scanner.ScanDirectoryAsync(_testDir);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(600, result.Size);
        Assert.Equal(3, result.FileCount);
        Assert.Equal(1, result.DirectoryCount);
        Assert.True(result.IsDirectory);
    }

    [Fact]
    public async Task ScanDirectoryAsync_WhenCancelled_ReturnsNull()
    {
        // Arrange
        var scanner = new FastParallelScanner();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await scanner.ScanDirectoryAsync(_testDir, cancellationToken: cts.Token);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors on temp dir
        }
    }
}