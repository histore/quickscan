# Requirements Specification: QuickScan

## Document Overview
This document serves as the single source of truth for all functional and non-functional requirements for **QuickScan** (formerly Space-Scan), a modern, high-performance disk space analyzer implemented in **C# 14** targeting **.NET 10** with **Avalonia UI 12**.

All features and system modifications must adhere to the Given-When-Then acceptance criteria outlined below.

---

## 1. System Architecture & Foundation

### REQ-QS-001: Clean Architecture Layer Separation
- **User Story**: As a software engineer, I want the system divided into decoupled Clean Architecture layers, so that business logic remains testable and independent of UI frameworks and external I/O mechanisms.
- **Acceptance Criteria**:
  - **Given** the QuickScan solution,
  - **When** reviewing dependency references across projects,
  - **Then** `QuickScan.Domain` must have zero external dependencies.
  - **And** `QuickScan.Application` depends only on `QuickScan.Domain`.
  - **And** `QuickScan.Infrastructure` implements application contracts (`IScanEngine`, `IScanCache`, `IDriveService`, `IFileSystemLauncher`).
  - **And** `QuickScan.UI` consumes application services and ViewModels via Dependency Injection (`Microsoft.Extensions.DependencyInjection`).
- **Impact & Consistency Check**: Consistent with core architecture rules in `ARCHITECTURE.md`.
- **Out of Scope**: Direct UI dependencies inside Domain or Application layers.

### REQ-QS-002: Dynamic Bilingual Internationalization (i18n & l10n)
- **User Story**: As an end user, I want to use the application in German or English and switch languages dynamically at runtime, so that all interface elements are localized without restarting.
- **Acceptance Criteria**:
  - **Given** the application running in either language,
  - **When** the user clicks the language button (`EN` or `DE`) in the top toolbar,
  - **Then** all user-facing labels, buttons, headers, tooltips, dialogs, and status messages update immediately.
  - **And** 0% user-facing strings may be hardcoded in C# or XAML code; all text must be defined in `Resources/Strings.de.axaml` and `Resources/Strings.en.axaml`.
- **Impact & Consistency Check**: Enforces workspace governance rule for 0% hardcoded strings.
- **Out of Scope**: Third-party language packs or runtime translation APIs.

---

## 2. Scanning Engine & Concurrency

### REQ-QS-003: Multicore Bounded Parallel Directory Traversal
- **User Story**: As a user, I want the scanner to utilize multicore CPU parallelism on SSDs and HDDs, so that large file trees are analyzed in the shortest possible time.
- **Acceptance Criteria**:
  - **Given** a directory path to scan,
  - **When** `FastParallelScanner.ScanDirectoryAsync` executes,
  - **Then** directories are traversed in parallel using bounded `Parallel.ForEach` tuned to CPU core count (`Environment.ProcessorCount`).
  - **And** file sizes, cumulative directory sizes, file counts, and directory counts are aggregated up the tree to the root `FsEntry`.
- **Impact & Consistency Check**: Fully replaces and matches the performance of Rust's `rayon` parallel scanner.
- **Out of Scope**: Distributed network scanning across multiple remote machines.

### REQ-QS-004: NTFS Junction & Symbolic Link Cycle Detection
- **User Story**: As a system administrator, I want directory traversal to safely ignore NTFS junctions and reparse points, so that infinite traversal loops and locked system errors are prevented.
- **Acceptance Criteria**:
  - **Given** a directory containing NTFS junctions or reparse points (`FileAttributes.ReparsePoint`),
  - **When** the scanner or lazy directory enumerator encounters the entry,
  - **Then** the reparse point is skipped and not followed recursively.
  - **And** `UnauthorizedAccessException` or I/O locks are caught gracefully without terminating the scan.
- **Impact & Consistency Check**: Essential for enterprise Windows stability and eliminating false positive crashes.
- **Out of Scope**: Modifying junction targets or resolving junction links.

### REQ-QS-005: Throttled Real-Time Scan Progress Feedback
- **User Story**: As a user, I want to see real-time scan progress without freezing or saturating the UI thread, so that the application remains responsive during large disk scans.
- **Acceptance Criteria**:
  - **Given** an ongoing scan traversing hundreds of thousands of files,
  - **When** directories are processed,
  - **Then** progress notifications are throttled (reported every 250 folders or 60ms).
  - **And** the status bar displays the current path, scanned folder count, scanned file count, and formatted byte count.
- **Impact & Consistency Check**: Prevents UI message queue saturation in Avalonia.
- **Out of Scope**: Writing scan logs to external disk files.

### REQ-QS-006: Asynchronous Non-Blocking Cancellation
- **User Story**: As a user, I want to cancel an active scan at any time, so that I can immediately start a different scan without waiting for the current scan to finish.
- **Acceptance Criteria**:
  - **Given** an active scan in progress,
  - **When** the user clicks the "Cancel" button (`CancelScanCommand`),
  - **Then** the active `CancellationTokenSource` is cancelled immediately.
  - **And** background worker threads stop traversal and the status returns to "Ready" (`Status.Ready`).
- **Impact & Consistency Check**: Eliminates orphaned background threads.
- **Out of Scope**: Pausing and resuming a partially finished scan.

### REQ-QS-007: Smart In-Memory Tree Caching
- **User Story**: As a user, I want previously scanned directories cached in memory, so that navigating back to visited folders is instantaneous without re-scanning.
- **Acceptance Criteria**:
  - **Given** a completed scan of a directory path,
  - **When** the user navigates away and subsequently returns to that directory path (or its subdirectories),
  - **Then** `ScanService` retrieves the existing `FsEntry` from `IScanCache` (`InMemoryScanCache`).
  - **And** the UI updates immediately without triggering `IScanEngine`.
  - **And** when the user clicks the "Refresh" button (`RefreshCommand`), the cache is bypassed (`forceScan: true`) and a fresh scan is executed.
- **Impact & Consistency Check**: Matches Rust's `fill_cache` and `HashMap` in-memory caching model.
- **Out of Scope**: On-disk persistent cache serialization across application restarts.

---

## 3. Application Startup & Scanning Lifecycle

### REQ-QS-008: Idle Initial Startup State (No Unsolicited Auto-Scan)
- **User Story**: As a user, I want the application to start in an idle, ready state without scanning automatically, so that I can choose which drive or folder to analyze before consuming system resources.
- **Acceptance Criteria**:
  - **Given** the application launches and the main window opens,
  - **When** the window loads (`OnMainWindowLoaded`),
  - **Then** no automatic scan of `C:\` or the current directory is initiated.
  - **And** `IsScanning` is `false`.
  - **And** the status bar displays "Ready" (`Status.Ready`).
  - **And** the central panel displays the initial prompt: "Select a drive or folder in the explorer to start scanning." (`Status.SelectToStart`).
- **Impact & Consistency Check**: Explicitly addresses user issue where startup launched an unsolicited scan of C:\.
- **Out of Scope**: Automatically resuming the last scanned path from a previous session.

### REQ-QS-009: Explicit Scan Initiation
- **User Story**: As a user, I want scans to start only upon my explicit selection, so that I have complete control over scan operations.
- **Acceptance Criteria**:
  - **Given** the application in an idle ready state,
  - **When** the user performs any of the following explicit actions:
    1. Clicks a drive button in the top toolbar (e.g. `C:\`, `D:\`),
    2. Clicks "Select Drive/Folder" (`SelectFolderCommand`) and selects a directory in the folder picker,
    3. Clicks on the name of a folder or drive in the Dateibaum sidebar (`OnTreeNodeTapped`),
    4. Right-clicks a tree node and selects "Scan Folder" (`Button.ScanFolder`),
    5. Selects a tree node and presses `Enter` or clicks the sidebar scan button,
  - **Then** and only then does the scan start for the chosen path.
- **Impact & Consistency Check**: Resolves all ambiguity regarding scan triggers.
- **Out of Scope**: Auto-scanning based on clipboard path changes.

---

## 4. File Explorer Sidebar (Dateibaum)

### REQ-QS-010: Collapsible Sidebar & Native Drive Listing
- **User Story**: As a user, I want a collapsible sidebar showing all system drives, so that I can navigate directory structures without cluttering the screen.
- **Acceptance Criteria**:
  - **Given** the main window,
  - **When** the application starts or drives change,
  - **Then** all available drives (e.g. `C:\`, `D:\`) are enumerated via `IDriveService` and displayed as root nodes in the tree view.
  - **And** pressing `F9` or clicking the sidebar toggle button toggles sidebar visibility (`IsSidebarVisible`).
- **Impact & Consistency Check**: Matches Rust's `show_sidebar` and `get_drives`.
- **Out of Scope**: Network share discovery without drive mapping.

### REQ-QS-011: Lazy Subdirectory Loading Without Triggering Scans
- **User Story**: As a user, I want expanding a folder in the tree view to reveal its subfolders without initiating a scan, so that I can browse folder hierarchies freely.
- **Acceptance Criteria**:
  - **Given** a directory node in the tree sidebar,
  - **When** the user clicks the expander chevron (`>`) or expands the node,
  - **Then** `IsExpanded` is set to `true` via two-way binding on `TreeViewItem`.
  - **And** `EnsureChildrenLoaded` queries direct child directories (via cache or `DirectoryInfo.EnumerateDirectories`) and populates `Children`.
  - **And** no scan is triggered, no files are scanned, and `IsScanning` remains `false`.
- **Impact & Consistency Check**: Resolves defect where expanding nodes triggered scans or failed to load child nodes.
- **Out of Scope**: Searching within collapsed tree nodes.

### REQ-QS-012: Single-Click Folder Name Scan Trigger
- **User Story**: As a user, I want a single click on a folder name in the Dateibaum to start scanning that folder, while clicking the chevron only expands or collapses it.
- **Acceptance Criteria**:
  - **Given** the directory tree sidebar,
  - **When** the user clicks on the expander chevron (`>`),
  - **Then** the branch expands or collapses without starting a scan.
  - **When** the user clicks on the name or icon of the folder (`OnTreeNodeTapped`),
  - **Then** `StartScanAsync(node.Path)` is called and the scan begins immediately.
- **Impact & Consistency Check**: Confirmed by user requirement for ergonomic one-click navigation.
- **Out of Scope**: Drag and drop reordering of tree nodes.

---

## 5. Interactive Usage Bar Chart

### REQ-QS-013: Proportional Color-Coded Usage Bars
- **User Story**: As a user, I want visual bar charts color-coded by item type and sized proportionally, so that I can immediately identify disk space hogs.
- **Acceptance Criteria**:
  - **Given** a scanned directory with child items,
  - **When** the bar chart renders (`BarListBox`),
  - **Then** directories display with blue bars (`#89b4fa`) and files display with green bars (`#a6e3a1`).
  - **And** bar widths are calculated proportionally relative to the largest item in the current view (`PercentageWidthConverter`).
  - **And** each row displays the icon (`📁`/`📄`), name, formatted size (e.g. `12.4 GB`), percentage of parent, and item count.
- **Impact & Consistency Check**: 1:1 visual parity with Rust's `show_bar_chart`.
- **Out of Scope**: Circular treemap charts or pie charts.

### REQ-QS-014: Chart Directory Diving & Upward Navigation
- **User Story**: As a user, I want to navigate into folders in the chart with a single click to inspect their subdirectories, and navigate back up easily.
- **Acceptance Criteria**:
  - **Given** a directory row in the bar chart,
  - **When** the user single-clicks or taps the row (`OnBarItemTapped`), or presses `Enter`,
  - **Then** the application navigates into that directory, scanning or retrieving it from cache.
  - **When** the user clicks the "Go Up" button (⬆) or presses `Alt + Up`,
  - **Then** the application navigates to the parent directory (`Directory.GetParent(CurrentPath)`).
  - **And** when at the root of a drive, the "Go Up" button is disabled (`CanGoUp == false`).
- **Impact & Consistency Check**: Ergonomic single-click navigation requested by user.
- **Out of Scope**: History back/forward browser-style button stack.

### REQ-QS-015: Multi-Criteria Column Sorting
- **User Story**: As a user, I want to sort entries by Size, Name, or Item count in ascending or descending order, so that I can organize listings according to my needs.
- **Acceptance Criteria**:
  - **Given** a populated bar chart view,
  - **When** the user clicks on the "Name", "Size", or "Items" column header,
  - **Then** the list sorts by the selected column.
  - **And** clicking the same column again toggles between descending and ascending order.
  - **And** the bar chart view refreshes immediately to reflect the new sort order.
- **Impact & Consistency Check**: Matches and extends Rust's sorting capabilities (`SortBy`, `SortOrder`).
- **Out of Scope**: Multi-column secondary sorting.

---

## 6. File Details View

### REQ-QS-016: Dedicated File Details Screen
- **User Story**: As a user, I want selecting a file in the chart to display a comprehensive details screen, so that I can examine file metadata and open the file.
- **Acceptance Criteria**:
  - **Given** a file row in the bar chart,
  - **When** the user selects or navigates to the file (`NavigateToItemAsync`),
  - **Then** `CurrentRoot` is set to the file entry (`IsDirectory == false`).
  - **And** the bar chart is replaced by a centered File Details card (`IsFileDetailsVisible == true`).
  - **And** the card displays:
    - Heading: `📄 Dateidetails` / `📄 File Details`
    - Name: file name
    - Path: absolute path
    - Size: formatted size in bytes/KB/MB/GB
    - Created: formatted creation timestamp
    - Modified: formatted modification timestamp
  - **And** clicking "Go Up" (⬆) navigates back to the containing folder.
- **Impact & Consistency Check**: Resolves parity gap with Rust's `show_file_details`.
- **Out of Scope**: Hex editing or inline file viewing.

### REQ-QS-017: File Opening & File Manager Integration
- **User Story**: As a user, I want buttons to launch the file or reveal it in the operating system file manager, so that I can inspect or edit it in native applications.
- **Acceptance Criteria**:
  - **Given** the File Details view or a context menu on an item,
  - **When** the user clicks "Open File" (`OpenCurrentFileCommand`),
  - **Then** the file is opened using the default registered application via `ProcessFileSystemLauncher.OpenFile`.
  - **When** the user clicks "Open in Explorer" (`OpenInExplorerCommand`),
  - **Then** the containing directory is opened in Windows Explorer via `ProcessFileSystemLauncher.OpenInFileManager`.
- **Impact & Consistency Check**: Cross-platform system launcher integration.
- **Out of Scope**: Embedding an internal document viewer.

---

## 7. Status, Feedback & Metadata

### REQ-QS-018: Status Bar & Aggregate Metrics
- **User Story**: As a user, I want aggregate statistics displayed at the bottom of the window, so that I have a quick summary of total folders, files, and disk space.
- **Acceptance Criteria**:
  - **Given** a completed directory scan,
  - **When** the view updates,
  - **Then** the status bar displays:
    - Scanned folder count (`TotalFolders`)
    - Scanned file count (`TotalFiles`)
    - Total formatted size (`FormattedTotalSize`)
  - **And** while a scan is active, an indeterminate progress bar and current scanning path are displayed.
- **Impact & Consistency Check**: Clear user feedback during and after scans.
- **Out of Scope**: CPU and RAM performance graphs in the status bar.

### REQ-QS-019: Modal About Dialog & Legal Licensing
- **User Story**: As a user or auditor, I want access to author information, license terms, and repository links, so that software provenance and open-source terms are verifiable.
- **Acceptance Criteria**:
  - **Given** the main window,
  - **When** the user clicks the "About" button (`ToggleAboutDialogCommand`),
  - **Then** a modal dialog overlay appears displaying:
    - Title: QuickScan
    - Author: Heino Stömmer
    - License: Apache License 2.0 (referencing `LICENSE` file)
    - Repository: `github.com/histore/quickscan`
  - **And** clicking "Close" dismisses the modal overlay.
- **Impact & Consistency Check**: Fully compliant with Apache 2.0 distribution notice requirements.
- **Out of Scope**: In-app online version update checker.

---

## 8. Selected Item Header & Explorer Synchronization

### REQ-QS-020: Selected Folder Header Banner with Disk Space Metrics
- **User Story**: As a user, I want the header banner above the main content area to display the selected folder's icon, name, and total disk space, so that I immediately see disk usage at a glance matching the reference application.
- **Acceptance Criteria**:
  - **Given** a scanned folder or selected item,
  - **When** the main panel displays its content,
  - **Then** the header banner displays the item icon (`📁` / `📄`), the folder/file name in bold, and the formatted disk space in parentheses (e.g. `📁 Windows (28.4 GB)`).
  - **And** below the title row, the full path is displayed with an "Open in Explorer" button.
  - **And** when no folder has been scanned yet (`CurrentPath` is empty), the banner is hidden.
- **Impact & Consistency Check**: Matches the Rust reference header (`📁 {} ({})`).
- **Out of Scope**: Editing folder names directly in the header banner.

### REQ-QS-021: Explorer Dateibaum Active Folder Tracking (Tree Follows Selection)
- **User Story**: As a user, I want the Dateibaum sidebar to automatically follow and visibly highlight the active folder navigated to in the main window, so that the tree view remains clearly synchronized with the current directory.
- **Acceptance Criteria**:
  - **Given** the Dateibaum sidebar and a directory navigated to in the main view (via scan, click on chart, drive selection, or Go Up),
  - **When** navigation is initiated or completed,
  - **Then** `SynchronizeTreeSelection` automatically expands all ancestor directory nodes along the path (`IsExpanded = true`).
  - **And** all non-matching nodes are deselected (`ClearTreeSelection`).
  - **And** the target directory node is marked as active (`IsSelected = true` and `SelectedNode = node`).
  - **And** the active node is prominently styled with active background (`#313244`), accent border (`#89b4fa`), and bold accent text (`#89b4fa`).
  - **When** a file is selected, the containing parent directory is synchronized and highlighted in the tree.
- **Impact & Consistency Check**: Matches the Rust tree auto-follow behavior (`s.starts_with(path) && s != path`) and accent styling.
- **Out of Scope**: Custom manual tree multi-selection.

