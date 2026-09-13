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

The project strictly adheres to **Clean Architecture**, **SOLID**, and **MVVM** principles:

- **[`QuickScan.Domain`](docs/architecture/modules/domain.md)**: Pure domain entities (`FsEntry`, `SortBy`, `SortOrder`, `ScanProgress`) and size formatting with zero external dependencies.
- **[`QuickScan.Application`](docs/architecture/modules/application.md)**: Service contracts (`IScanEngine`, `IDriveService`, `IFileSystemLauncher`, `IScanCache`, `ILocalizationService`) and orchestration services (`ScanService`).
- **[`QuickScan.Infrastructure`](docs/architecture/modules/infrastructure.md)**: Multi-core parallel scan engine (`FastParallelScanner`), drive enumeration (`WindowsDriveService`), in-memory caching (`InMemoryScanCache`), and OS launchers.
- **[`QuickScan.UI`](docs/architecture/modules/ui.md)**: Avalonia MVVM presentation powered by `CommunityToolkit.Mvvm`, compiled bindings, dynamic bilingual resources, and Dependency Injection.
- **`QuickScan.Tests`**: Comprehensive automated test suite using xUnit covering domain models, caching, scanning logic, and application orchestration.

For comprehensive architectural blueprints, sequence diagrams, and cross-cutting rules, see [ARCHITECTURE.md](ARCHITECTURE.md) and the modular specifications in [`docs/architecture/modules/`](docs/architecture/modules/).

## Getting Started 🛠️

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)

### Repository Setup (Git Submodules)
This repository utilizes shared development and agent skills as a Git submodule at `_agents`:
```powershell
# Clone repository with submodules
git clone --recurse-submodules https://github.com/histore/quickscan.git

# Or initialize submodules in an existing clone
git submodule update --init --recursive
```

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