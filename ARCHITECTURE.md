# Architecture Documentation: QuickScan Sharp

## Overview
**QuickScan Sharp** is a modular, high-performance disk space analyzer implemented in **C# 14** targeting **.NET 10** with **Avalonia UI 12**. It reimplements the architecture of the Rust `quickscan` application while leveraging .NET multi-threading, Clean Architecture, MVVM design patterns, and dynamic bilingual localization.

## Core Design Principles
1. **Clean Architecture**: Strict separation of concerns across layers. Domain models remain independent of external libraries or UI frameworks.
2. **Clean Code & SOLID**: Highly focused classes, immutable records where appropriate, explicit interfaces, and descriptive naming.
3. **High-Throughput Concurrency**: Multi-core recursive file system traversal via bounded parallelism (`Parallel.ForEach`) with throttled progress updates to avoid UI saturation.
4. **Resilience & Safety**:
   - Prevention of infinite loops across NTFS junctions and symbolic links (`FileAttributes.ReparsePoint`).
   - Graceful handling of `UnauthorizedAccessException` and I/O locks.
   - Non-blocking asynchronous cancellation support via `CancellationToken`.
5. **0% Hardcoded UI Strings**: Full bilingual internationalization (German and English) utilizing dynamic Avalonia Resource Dictionaries.

## System Layers

### 1. Domain Layer (`QuickScan.Domain`)
Contains core business entities, value objects, and domain logic with zero external dependencies:
- **`FsEntry`**: Hierarchical representation of directories and files containing metadata (size, file counts, directory counts, timestamps, children). Supports tree traversal, path searches, and multi-criteria sorting.
- **`ScanProgress`**: Record containing real-time scan metrics (current path, folder counter, file counter, byte counter, completion state).
- **`SortBy` & `SortOrder`**: Enums defining sorting dimensions and directions.
- **`ByteSizeFormatter`**: Value formatter with invariant culture formatting for storage sizes.

### 2. Application Layer (`QuickScan.Application`)
Defines the abstract contracts and use cases:
- **Contracts**:
  - `IScanEngine`: Abstraction for recursive directory scanning.
  - `IScanCache`: In-memory caching interface for fast hierarchical tree lookup.
  - `IDriveService`: Storage drive enumeration contract.
  - `IFileSystemLauncher`: Contract for opening directories or files in the native desktop environment.
  - `ILocalizationService`: Contract for dynamic language switching.
- **Services**:
  - `ScanService`: Coordinates scan execution, cache hits, progress aggregation, cancellation, and parent navigation (`GoUpAsync`).

### 3. Infrastructure Layer (`QuickScan.Infrastructure`)
Contains concrete implementations of external services:
- **`FastParallelScanner`**: Implements `IScanEngine`. Leverages multi-core parallel processing, throttled UI progress reporting (every 250 folders or 60ms), and cycle detection for symbolic links and junctions.
- **`InMemoryScanCache`**: Thread-safe caching utilizing `ConcurrentDictionary<string, FsEntry>` with case-insensitive path comparisons.
- **`WindowsDriveService`**: Detects system drives using `System.IO.DriveInfo`.
- **`ProcessFileSystemLauncher`**: Opens files and paths using system shell (`explorer.exe`, `xdg-open`, or macOS `open`).

### 4. Presentation Layer (`QuickScan.UI`)
Avalonia MVVM application powered by `CommunityToolkit.Mvvm`:
- **`App`**: Sets up the IoC container via `Microsoft.Extensions.DependencyInjection` and registers services and view models.
- **`MainViewModel`**: Manages user actions, scan requests, directory tree state, drive listings, sorting, and language switching.
- **`BarItemViewModel`**: Visual representation of proportional usage bars (Blue for folders, Green for files) with calculated percentage widths.
- **`ExplorerNodeViewModel`**: On-demand hierarchical directory node for the explorer sidebar.
- **`MainWindow.axaml`**: Declarative UI layout with top toolbar, collapsible sidebar, interactive bar chart list, status bar, and modal about dialog.
- **`Resources/Strings.en.axaml` & `Resources/Strings.de.axaml`**: Localized bilingual strings.

## Data Flow Diagram

```text
[User Action: Select Drive / Folder]
               │
               ▼
      [MainViewModel]
               │
               ▼
         [ScanService] ──── Check Cache ────► [InMemoryScanCache]
               │                                       │
         (Cache Miss)                             (Cache Hit)
               │                                       │
               ▼                                       ▼
     [FastParallelScanner]                     [Immediate UI Update]
    (Parallel.ForEach on SSD)
               │
         Progress Events (Throttled)
               │
               ▼
      [Status Bar & Metrics]
               │
               ▼
     [Hierarchical FsEntry]
               │
               ▼
       [Update Cache]
               │
               ▼
   [Populate Bar Chart & Tree]
```

## Automated Testing (`QuickScan.Tests`)
Includes automated unit tests using **xUnit** following the Arrange-Act-Assert (AAA) pattern:
- Domain models and tree queries (`FsEntryTests`, `ByteSizeFormatterTests`).
- Infrastructure services and parallel directory scanning (`FastParallelScannerTests`, `InMemoryScanCacheTests`).
- Application orchestration and caching (`ScanServiceTests`).