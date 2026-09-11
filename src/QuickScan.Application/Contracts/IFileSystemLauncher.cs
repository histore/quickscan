namespace QuickScan.Application.Contracts;

/// <summary>
/// Contract for launching files and opening directory paths in the operating system.
/// </summary>
public interface IFileSystemLauncher
{
    /// <summary>
    /// Opens the specified folder or highlights the specified item in the native file manager.
    /// </summary>
    /// <param name="path">The file or folder path.</param>
    void OpenInFileManager(string path);

    /// <summary>
    /// Opens the file with its default system application.
    /// </summary>
    /// <param name="filePath">The file path to launch.</param>
    void OpenFile(string filePath);
}