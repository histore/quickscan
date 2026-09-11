# QuickScan Sharp 🚀

A modern, high-performance cross-platform interactive disk space analyzer built with **C# 14**, **.NET 10**, and **Avalonia UI 12**.

Reimplemented from the original Rust `quickscan` project to provide full enterprise compatibility, eliminate Falcon CrowdStrike false positives, and introduce rich Fluent dark UI with dynamic internationalization.

## Key Features ✨

- **Modern Avalonia UI**: Clean, responsive interface featuring dark theme styling, rounded containers, and smooth layouts.
- **True Multi-Core Parallel Scanning**: Highly optimized recursive traversal powered by `Parallel.ForEach` across all CPU cores with bounded parallelism and symlink loop protection.
- **Interactive Proportional Bar Charts**: Color-coded usage bars (Blue for Folders, Green for Files) with real-time percentage indicators and hover details.
- **Smart In-Memory Caching**: Scanned hierarchies are cached in memory for instantaneous navigation without re-scanning.
- **Full File Explorer Sidebar**:
  - Collapsible persistent sidebar with drive list and recursive directory tree.
  - Shortcut key: **F9** toggles the explorer sidebar.
- **Navigation Controls**:
  - "Go Up" (⬆) navigation button with **Alt+Up** keyboard shortcut.
  - Quick drive selector chips.
  - Open selected folder/file directly in native Windows Explorer / file manager.
- **0% Hardcoded UI Strings (i18n & l10n)**:
  - Full bilingual support in **English (`en`)** and **German (`de`)**.
  - Dynamic runtime language switching via toolbar buttons.
- **Live Scan Feedback**:
  - Real-time status bar displaying current path, scanned folder count, file count, and aggregated size.
  - Responsive "Cancel" button to abort long-running scans.

## Architecture 🏛️

The project strictly follows **Clean Architecture** and **Clean Code** principles:

- `QuickScan.Domain`: Pure domain models (`FsEntry`, `SortBy`, `SortOrder`, `ScanProgress`) with zero external dependencies.
- `QuickScan.Application`: Application contracts (`IScanEngine`, `IDriveService`, `IFileSystemLauncher`, `IScanCache`, `ILocalizationService`) and orchestration services (`ScanService`).
- `QuickScan.Infrastructure`: Multi-core scanning engine (`FastParallelScanner`), drive enumeration (`WindowsDriveService`), in-memory caching (`InMemoryScanCache`), and process launchers.
- `QuickScan.UI`: Avalonia MVVM application powered by `CommunityToolkit.Mvvm`, compiled bindings, dynamic resource dictionaries, and Dependency Injection.
- `QuickScan.Tests`: Comprehensive test suite using xUnit covering domain models, caching, scanning logic, and application services.

## Getting Started 🛠️

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)

### Building and Running
```powershell
# Build solution
dotnet build

# Run unit tests
dotnet test

# Launch application
dotnet run --project src/QuickScan.UI
```

## License ⚖️

This project is licensed under the **Apache License 2.0**.