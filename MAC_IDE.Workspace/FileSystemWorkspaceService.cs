using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MAC_IDE.Core.Interfaces;
using MAC_IDE.Core.Models;

namespace MAC_IDE.Workspace;

public class FileSystemWorkspaceService : IWorkspaceService
{
    public string? CurrentRootPath { get; private set; }
    public bool HasOpenWorkspace => !string.IsNullOrEmpty(CurrentRootPath);

    public Task<ProjectItemNode> OpenWorkspaceFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"El directorio no existe: {folderPath}");

        CurrentRootPath = folderPath;
        var rootNode = BuildDirectoryTree(new DirectoryInfo(folderPath));
        return Task.FromResult(rootNode);
    }

    public void CloseWorkspace()
    {
        CurrentRootPath = null;
    }

    private ProjectItemNode BuildDirectoryTree(DirectoryInfo dirInfo)
    {
        var node = new ProjectItemNode
        {
            Name = dirInfo.Name,
            FullPath = dirInfo.FullName,
            Type = ProjectItemType.Folder
        };

        try
        {
            // Subcarpetas (filtrando carpetas pesadas/ocultas)
            var subDirs = dirInfo.GetDirectories()
                .Where(d => !d.Name.StartsWith(".") && d.Name != "bin" && d.Name != "obj" && d.Name != "node_modules")
                .OrderBy(d => d.Name);

            foreach (var dir in subDirs)
            {
                node.Children.Add(BuildDirectoryTree(dir));
            }

            // Archivos
            var files = dirInfo.GetFiles().OrderBy(f => f.Name);
            foreach (var file in files)
            {
                node.Children.Add(new ProjectItemNode
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    Type = ProjectItemType.File
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Omitir carpetas sin permisos del sistema
        }

        return node;
    }
}