using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickScan.Application.Contracts;
using QuickScan.Domain.Models;

namespace QuickScan.Application.Services;

/// <summary>
/// Orchestrates file system scans, caching, and navigation.
/// </summary>
public sealed class ScanService : IDisposable
{
    private readonly IScanEngine _scanEngine;
    private readonly IScanCache _scanCache;
    private CancellationTokenSource? _currentCts;
    private readonly object _lock = new();

    /// <summary>
    /// Event triggered when a scan completes successfully with a new root entry.
    /// </summary>
    public event Action<FsEntry>? ScanCompleted;

    /// <summary>
    /// Event triggered when scan progress is updated.
    /// </summary>
    public event Action<ScanProgress>? ProgressChanged;

    /// <summary>
    /// Event triggered when scan state changes (e.g. started, finished, cancelled).
    /// </summary>
    public event Action<bool>? ScanningStateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanService"/> class.
    /// </summary>
    public ScanService(IScanEngine scanEngine, IScanCache scanCache)
    {
        _scanEngine = scanEngine ?? throw new ArgumentNullException(nameof(scanEngine));
        _scanCache = scanCache ?? throw new ArgumentNullException(nameof(scanCache));
    }

    /// <summary>
    /// Gets a value indicating whether a scan is currently active.
    /// </summary>
    public bool IsScanning { get; private set; }

    /// <summary>
    /// Gets the currently active root entry if any.
    /// </summary>
    public FsEntry? CurrentRoot { get; private set; }

    /// <summary>
    /// Gets the currently selected path.
    /// </summary>
    public string? CurrentPath { get; private set; }

    /// <summary>
    /// Navigates to a path. If cached and not forced, loads immediately from cache; otherwise initiates a scan.
    /// </summary>
    public async Task StartScanAsync(string path, bool forceScan = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var trimmed = path.Trim();
        if (trimmed.Length == 2 && trimmed[1] == ':')
        {
            trimmed += Path.DirectorySeparatorChar;
        }

        var normalizedPath = Path.GetFullPath(trimmed);
        var root = Path.GetPathRoot(normalizedPath);
        if (!string.Equals(normalizedPath, root, StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        if (!forceScan && _scanCache.TryGet(normalizedPath, out var cachedEntry) && cachedEntry is not null)
        {
            CancelScan();
            CurrentRoot = cachedEntry;
            CurrentPath = normalizedPath;
            ScanCompleted?.Invoke(cachedEntry);
            return;
        }

        CancellationTokenSource cts;
        CancellationToken token;
        lock (_lock)
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _currentCts = new CancellationTokenSource();
            cts = _currentCts;
            token = cts.Token;
            IsScanning = true;
        }

        ScanningStateChanged?.Invoke(true);

        var progressReporter = new Progress<ScanProgress>(p =>
        {
            if (!token.IsCancellationRequested)
            {
                ProgressChanged?.Invoke(p);
            }
        });

        try
        {
            var result = await _scanEngine.ScanDirectoryAsync(normalizedPath, progressReporter, token).ConfigureAwait(false);
            if (result is not null && !token.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_currentCts, cts))
                    {
                        CurrentRoot = result;
                        CurrentPath = normalizedPath;
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    _scanCache.Set(result);
                    ScanCompleted?.Invoke(result);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled intentionally
        }
        finally
        {
            bool shouldNotifyStateChange = false;
            lock (_lock)
            {
                if (ReferenceEquals(_currentCts, cts))
                {
                    IsScanning = false;
                    _currentCts = null;
                    shouldNotifyStateChange = true;
                }
            }

            if (shouldNotifyStateChange)
            {
                ScanningStateChanged?.Invoke(false);
            }

            cts.Dispose();
        }
    }

    /// <summary>
    /// Cancels the currently running scan.
    /// </summary>
    public void CancelScan()
    {
        lock (_lock)
        {
            _currentCts?.Cancel();
        }
    }

    /// <summary>
    /// Navigates to the parent folder of the currently focused path.
    /// </summary>
    public async Task<bool> GoUpAsync()
    {
        if (IsScanning || string.IsNullOrEmpty(CurrentPath))
        {
            return false;
        }

        var parent = Directory.GetParent(CurrentPath);
        if (parent is null)
        {
            return false;
        }

        await StartScanAsync(parent.FullName, forceScan: false);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }
}