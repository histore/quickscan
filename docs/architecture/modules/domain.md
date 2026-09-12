# Domain Module (`QuickScan.Domain`)

## Purpose & Scope
The **Domain Layer** encapsulates the foundational entities, value representations, and business logic of the QuickScan Sharp system. It maintains absolute independence from external frameworks, UI toolkits, or operating system APIs, adhering strictly to Clean Architecture principles.

---

## Component Structure

```text
QuickScan.Domain/
├── Common/
│   └── ByteSizeFormatter.cs
└── Models/
    ├── FsEntry.cs
    ├── ScanProgress.cs
    ├── SortBy.cs
    └── SortOrder.cs
```

---

## Key Types & Contracts

### 1. [`FsEntry`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Domain/Models/FsEntry.cs)
Hierarchical tree node representing either a file system directory or an individual file.
- **Properties**:
  - `Name`: File or folder name.
  - `Path`: Absolute file system path.
  - `Size`: Aggregated total size in bytes (recursive sum for directories).
  - `FileCount`: Total count of files contained directly and recursively.
  - `DirectoryCount`: Total count of subdirectories contained recursively.
  - `IsDirectory`: Boolean indicating if the node represents a container directory.
  - `LastModified`: UTC/local timestamp of last modification.
  - `Children`: Read-only or mutable collection of child [`FsEntry`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Domain/Models/FsEntry.cs) nodes.
- **Operations**:
  - `AddChild(FsEntry child)`: Adds child node and updates aggregated statistics.
  - `FindNode(string path)`: Recursively locates a descendant node matching the given path.
  - `SortChildren(SortBy sortBy, SortOrder order)`: In-place sorting of children by size, name, item count, or timestamp.

### 2. [`ScanProgress`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Domain/Models/ScanProgress.cs)
Immutable record providing point-in-time metrics during an active scan:
- `CurrentPath`: The directory path currently being traversed.
- `FoldersScanned`: Running counter of processed directories.
- `FilesScanned`: Running counter of processed files.
- `BytesScanned`: Running counter of accumulated bytes.
- `IsComplete`: Flag indicating final scan completion.

### 3. [`SortBy`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Domain/Models/SortBy.cs) & [`SortOrder`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Domain/Models/SortOrder.cs)
Domain enumerations defining sort criteria:
- **`SortBy`**: `Size`, `Name`, `Count` (item count), `Date` (last modified).
- **`SortOrder`**: `Ascending`, `Descending`.

### 4. [`ByteSizeFormatter`](file:///c:/projekte/csharp/quickscan/src/QuickScan.Domain/Common/ByteSizeFormatter.cs)
Utility class providing localized and invariant culture formatting of raw byte counts into human-readable representations (`B`, `KB`, `MB`, `GB`, `TB`, `PB`) using standard binary (1024) or decimal multipliers.

---

## Architectural Invariants
- **Zero External Dependencies**: Only depends on standard .NET runtime types (`System`, `System.Collections.Generic`).
- **Immutability & Safety**: State changes within `FsEntry` tree nodes are thread-confined during construction and read-only once published to presentation.
- **No I/O Operations**: The domain layer never performs disk access, network calls, or platform-specific Win32 operations.
