using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuickScan.Application.Contracts;
using QuickScan.Domain.Models;

namespace QuickScan.Infrastructure.Scanning;

/// <summary>
/// High-performance parallel file system scanner utilizing multi-core parallelism.
/// </summary>
public sealed class FastParallelScanner : IScanEngine
{
    private readonly int _maxDegreeOfParallelism;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastParallelScanner"/> class.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum concurrency level. Defaults to processor count.</param>
    public FastParallelScanner(int? maxDegreeOfParallelism = null)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism ?? Math.Max(2, Environment.ProcessorCount);
    }

    /// <inheritdoc/>
    public async Task<FsEntry?> ScanDirectoryAsync(
        string path,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        var state = new ScanState
        {
            LastReportTicks = Environment.TickCount64
        };

        void ReportProgress(string currentPath, bool force = false)
        {
            if (progress is null)
            {
                return;
            }

            var currentTicks = Environment.TickCount64;
            var folders = Interlocked.Read(ref state.FolderCounter);
            // Report every 250 folders or every 60ms
            if (force || folders % 250 == 0 || (currentTicks - Interlocked.Read(ref state.LastReportTicks) > 60))
            {
                Interlocked.Exchange(ref state.LastReportTicks, currentTicks);
                progress.Report(new ScanProgress(
                    CurrentPath: currentPath,
                    FoldersScanned: folders,
                    FilesScanned: Interlocked.Read(ref state.FileCounter),
                    BytesScanned: Interlocked.Read(ref state.ByteCounter),
                    IsCompleted: false,
                    IsCancelled: false));
            }
        }

        try
        {
            var result = await Task.Run(() => ScanRecursive(
                new DirectoryInfo(path),
                state,
                ReportProgress,
                cancellationToken), cancellationToken).ConfigureAwait(false);

            ReportProgress(path, force: true);
            return result;
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new ScanProgress(
                CurrentPath: path,
                FoldersScanned: Interlocked.Read(ref state.FolderCounter),
                FilesScanned: Interlocked.Read(ref state.FileCounter),
                BytesScanned: Interlocked.Read(ref state.ByteCounter),
                IsCompleted: false,
                IsCancelled: true));
            return null;
        }
    }

    private FsEntry? ScanRecursive(
        DirectoryInfo dirInfo,
        ScanState state,
        Action<string, bool> reportProgress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Interlocked.Increment(ref state.FolderCounter);
        reportProgress(dirInfo.FullName, false);

        var children = new List<FsEntry>();
        var subDirs = new List<DirectoryInfo>();

        try
        {
            // Enumerate files first
            foreach (var file in dirInfo.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                long fileSize = 0;
                DateTimeOffset? created = null;
                DateTimeOffset? modified = null;

                try
                {
                    fileSize = file.Length;
                    created = file.CreationTimeUtc;
                    modified = file.LastWriteTimeUtc;
                }
                catch
                {
                    // Ignore metadata access errors
                }

                Interlocked.Increment(ref state.FileCounter);
                Interlocked.Add(ref state.ByteCounter, fileSize);

                children.Add(new FsEntry(
                    path: file.FullName,
                    name: file.Name,
                    size: fileSize,
                    isDirectory: false,
                    createdAt: created,
                    modifiedAt: modified,
                    fileCount: 1,
                    directoryCount: 0,
                    children: null));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Access denied, continue with whatever entries we have
        }
        catch (DirectoryNotFoundException)
        {
            // Directory might have been removed
        }
        catch (IOException)
        {
            // I/O error
        }

        try
        {
            // Enumerate subdirectories
            foreach (var subDir in dirInfo.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip reparse points (symlinks, junctions) to avoid circular reference loops
                if ((subDir.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                subDirs.Add(subDir);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }

        // Process subdirectories in parallel
        if (subDirs.Count > 0)
        {
            var scannedSubDirs = new ConcurrentBag<FsEntry>();

            Parallel.ForEach(
                subDirs,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                },
                subDir =>
                {
                    var result = ScanRecursive(
                        subDir,
                        state,
                        reportProgress,
                        cancellationToken);

                    if (result is not null)
                    {
                        scannedSubDirs.Add(result);
                    }
                });

            children.AddRange(scannedSubDirs);
        }

        long totalSize = 0;
        long totalFiles = 0;
        long totalDirs = 0;

        foreach (var child in children)
        {
            totalSize += child.Size;
            totalFiles += child.FileCount;
            if (child.IsDirectory)
            {
                totalDirs += child.DirectoryCount + 1;
            }
        }

        DateTimeOffset? dirCreated = null;
        DateTimeOffset? dirModified = null;
        try
        {
            dirCreated = dirInfo.CreationTimeUtc;
            dirModified = dirInfo.LastWriteTimeUtc;
        }
        catch { }

        // Sort children initially by size descending
        var sortedChildren = children
            .OrderByDescending(c => c.Size)
            .ToList();

        return new FsEntry(
            path: dirInfo.FullName,
            name: string.IsNullOrEmpty(dirInfo.Name) ? dirInfo.FullName : dirInfo.Name,
            size: totalSize,
            isDirectory: true,
            createdAt: dirCreated,
            modifiedAt: dirModified,
            fileCount: totalFiles,
            directoryCount: totalDirs,
            children: sortedChildren);
    }

    private sealed class ScanState
    {
        public long FolderCounter;
        public long FileCounter;
        public long ByteCounter;
        public long LastReportTicks;
    }
}