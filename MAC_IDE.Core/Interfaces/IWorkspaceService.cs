using MAC_IDE.Core.Models;

namespace MAC_IDE.Core.Interfaces;

public interface IWorkspaceService
{
    string? CurrentRootPath { get; }
    bool HasOpenWorkspace { get; }

    Task<ProjectItemNode> OpenWorkspaceFolderAsync(string folderPath);
    void CloseWorkspace();
}