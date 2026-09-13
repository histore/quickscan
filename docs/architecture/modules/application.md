# Application Module (`QuickScan.Application`)

## Purpose & Scope
The **Application Layer** orchestrates system use cases and defines abstract service contracts. It bridges the pure business models of [`QuickScan.Domain`](domain.md) with external infrastructure and presentation layers, ensuring the core application logic remains testable with mocks or in-memory stubs.

---

## Component Structure

```text
QuickScan.Application/
├── Contracts/
│   ├── DriveItem.cs
│   ├── IDriveService.cs
│   ├── IFileSystemLauncher.cs
│   ├── ILocalizationService.cs
│   ├── IScanCache.cs
│   └── IScanEngine.cs
└── Services/
    └── ScanService.cs
```

---

## Key Contracts & Services

### 1. Service Contracts (`Contracts/`)
- **[`IScanEngine`](../../../src/QuickScan.Application/Contracts/IScanEngine.cs)**:
  Defines the asynchronous interface for recursive directory scanning:
  ```csharp
  Task<FsEntry> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
  ```
- **[`IScanCache`](../../../src/QuickScan.Application/Contracts/IScanCache.cs)**:
  Abstraction for caching scanned directory trees to enable instantaneous navigation without rescanning:
  ```csharp
  bool TryGet(string path, out FsEntry? entry);
  void Set(string path, FsEntry entry);
  void Clear();
  ```
- **[`IDriveService`](../../../src/QuickScan.Application/Contracts/IDriveService.cs)**:
  Retrieves storage volume information across supported operating systems:
  ```csharp
  Task<IReadOnlyList<DriveItem>> GetDrivesAsync(CancellationToken cancellationToken);
  ```
- **[`IFileSystemLauncher`](../../../src/QuickScan.Application/Contracts/IFileSystemLauncher.cs)**:
  Abstracts operating system file manager interactions (opening files or revealing paths in Windows Explorer / Finder / file managers):
  ```csharp
  Task OpenPathAsync(string path);
  Task OpenInFileManagerAsync(string path);
  ```
- **[`ILocalizationService`](../../../src/QuickScan.Application/Contracts/ILocalizationService.cs)**:
  Provides dynamic runtime language switching and notification:
  ```csharp
  string CurrentLanguage { get; }
  void SetLanguage(string languageCode);
  event EventHandler? LanguageChanged;
  ```

### 2. Domain Data Transfer Records
- **[`DriveItem`](../../../src/QuickScan.Application/Contracts/DriveItem.cs)**:
  Record encapsulating volume metrics:
  ```csharp
  public sealed record DriveItem(
      string Name,
      string VolumeLabel,
      string RootDirectory,
      long TotalSize,
      long AvailableFreeSpace,
      string DriveType
  );
  ```

### 3. Application Services (`Services/`)
- **[`ScanService`](../../../src/QuickScan.Application/Services/ScanService.cs)**:
  Coordinates the primary scanning workflow:
  1. Checks [`IScanCache`](../../../src/QuickScan.Application/Contracts/IScanCache.cs) for an existing scan result matching the target path.
  2. If absent, dispatches execution to [`IScanEngine`](../../../src/QuickScan.Application/Contracts/IScanEngine.cs) with progress aggregation.
  3. Caches successful scan results.
  4. Provides helper routines for parent navigation (`GoUpAsync`) and subtree resolution.

---

## Architectural Invariants
- **UI & Infrastructure Agnostic**: No references to Avalonia, Win32 P/Invoke, or filesystem concrete classes.
- **Async/Await & Cancellation**: All long-running operations accept a `CancellationToken` to ensure responsive UI cancellation.
