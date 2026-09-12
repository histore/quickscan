# Infrastructure Module (`QuickScan.Infrastructure`)

## Purpose & Scope
The **Infrastructure Layer** provides concrete implementations of the contracts defined in [`QuickScan.Application`](file:///c:/projekte/csharp/quickscan/docs/architecture/modules/application.md). It encapsulates all direct interactions with external systems: physical file system traversal, storage drive detection, caching mechanisms, and process execution.

---

## Component Structure

```text
QuickScan.Infrastructure/
├── Scanning/
│   └── FastParallelScanner.cs
└── Services/
    ├── InMemoryScanCache.cs
    ├── ProcessFileSystemLauncher.cs
    └── WindowsDriveService.cs
```

---

## Key Implementations

### 1. [`FastParallelScanner`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Infrastructure/Scanning/FastParallelScanner.cs)
High-throughput recursive directory scanner implementing [`IScanEngine`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Application/Contracts/IScanEngine.cs).
- **Concurrency Model**:
  - Leverages bounded multi-core parallelism using `Parallel.ForEach` across top-level subdirectories.
  - Recursively processes nested folder hierarchies with minimal allocation overhead.
- **Throttled Progress Reporting**:
  - Dispatches progress notifications through `IProgress<ScanProgress>` at controlled intervals (every 250 processed folders or ~60ms) to prevent message queue saturation on the UI thread.
- **Cycle & Loop Avoidance**:
  - Inspects file and directory attributes for `FileAttributes.ReparsePoint` to detect NTFS junctions and symbolic links, avoiding infinite recursion and redundant scans.
- **Fault Tolerance**:
  - Gracefully catches and recovers from `UnauthorizedAccessException`, `DirectoryNotFoundException`, and `PathTooLongException` without aborting the overall scan.

### 2. [`InMemoryScanCache`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Infrastructure/Services/InMemoryScanCache.cs)
Thread-safe caching implementation of [`IScanCache`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Application/Contracts/IScanCache.cs).
- Backed by `ConcurrentDictionary<string, FsEntry>` using `StringComparer.OrdinalIgnoreCase`.
- Provides lock-free lookups and insertions for instantaneous directory tree navigation.

### 3. [`WindowsDriveService`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Infrastructure/Services/WindowsDriveService.cs)
Drive detection service implementing [`IDriveService`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Application/Contracts/IDriveService.cs).
- Queries `System.IO.DriveInfo.GetDrives()`.
- Filters for ready drives (`IsReady == true`) and transforms drive metadata into immutable [`DriveItem`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Application/Contracts/DriveItem.cs) records.

### 4. [`ProcessFileSystemLauncher`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Infrastructure/Services/ProcessFileSystemLauncher.cs)
OS launcher implementing [`IFileSystemLauncher`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Application/Contracts/IFileSystemLauncher.cs).
- Interacts with the host environment using `Process.Start` with `ProcessStartInfo.UseShellExecute = true`.
- On Windows, uses `explorer.exe /select,"<path>"` to highlight files in Windows Explorer or open folder paths directly.

---

## Architectural Invariants
- **Platform Separation**: Platform-specific logic is isolated behind contracts.
- **Thread Safety**: Scanner and cache implementations are fully thread-safe and safe for concurrent invocations.
