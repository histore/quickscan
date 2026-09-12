# UI Module (`QuickScan.UI`)

## Purpose & Scope
The **Presentation Layer** implements the Avalonia MVVM user interface, delivering an interactive and visually polished disk space analysis experience. It leverages `CommunityToolkit.Mvvm` for observable state binding, commands, and dependency injection, with 0% hardcoded strings to ensure full German and English bilingual localization.

---

## Component Structure

```text
QuickScan.UI/
├── App.axaml / App.axaml.cs
├── Program.cs
├── ViewLocator.cs
├── Converters/
├── Models/
├── Resources/
│   ├── Strings.de.axaml
│   └── Strings.en.axaml
├── Services/
│   └── LocalizationService.cs
├── ViewModels/
│   ├── BarItemViewModel.cs
│   ├── ExplorerNodeViewModel.cs
│   ├── MainViewModel.cs
│   └── ViewModelBase.cs
└── Views/
    ├── MainWindow.axaml
    └── MainWindow.axaml.cs
```

---

## Key Components

### 1. Composition Root ([`App.axaml.cs`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/App.axaml.cs))
Initializes dependency injection via `Microsoft.Extensions.DependencyInjection`:
- Registers application services: `IScanEngine`, `IScanCache`, `IDriveService`, `IFileSystemLauncher`, `ILocalizationService`, `ScanService`.
- Registers presentation models: [`MainViewModel`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/ViewModels/MainViewModel.cs).
- Resolves the root `MainWindow` with its data context upon startup.

### 2. ViewModels (`ViewModels/`)
- **[`MainViewModel`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/ViewModels/MainViewModel.cs)**:
  Central state coordinator:
  - Manages drive listings, selected target directory, and scan execution/cancellation.
  - Maintains the collection of proportional [`BarItemViewModel`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/ViewModels/BarItemViewModel.cs) items for the active directory.
  - Coordinates folder navigation (`NavigateInto`, `NavigateUp`, path breadcrumbs).
  - Handles dynamic sorting by size, name, count, or date.
  - Binds live metrics from `ScanProgress` during scanning.
- **[`BarItemViewModel`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/ViewModels/BarItemViewModel.cs)**:
  Represents an individual item in the visual distribution chart:
  - Formatted byte size, item count, percentage of parent directory.
  - Proportional bar width and distinct styling (directories vs. individual files).
- **[`ExplorerNodeViewModel`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/ViewModels/ExplorerNodeViewModel.cs)**:
  Hierarchical node for the collapsible sidebar directory tree, populating child nodes on demand.

### 3. Views (`Views/`)
- **[`MainWindow.axaml`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/Views/MainWindow.axaml)**:
  Modern Avalonia declarative UI layout:
  - Top toolbar: Quick drive selection buttons, folder browser button, sort criteria dropdown, language toggle button.
  - Main workspace: Split view with collapsible directory navigation sidebar and responsive proportional bar chart list.
  - Bottom status bar: Real-time scan statistics (current path, scanned directories/files, total bytes processed, execution time).
  - Compiled bindings (`x:DataType`) for high performance and zero reflection overhead.

### 4. Dynamic Localization ([`LocalizationService.cs`](file:///c:/projekte/csharp/quickscan/src/QuickScan.UI/Services/LocalizationService.cs))
- Dynamic runtime language switching between German (`de`) and English (`en`).
- Swaps the active XAML resource dictionary in `Application.Current.Resources.MergedDictionaries`.
- All user-facing strings are bound via dynamic resource lookups (`{DynamicResource StringKey}`).

---

## Architectural Invariants
- **Compiled Bindings**: All XAML views enforce compiled bindings (`x:DataType="vm:MainViewModel"`).
- **Decoupled ViewModels**: ViewModels contain no direct UI controls or platform-specific references.
- **Thread Marshaling**: Background progress updates are dispatched cleanly to UI-bound observable properties.
