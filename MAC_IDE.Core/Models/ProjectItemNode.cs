namespace MAC_IDE.Core.Models;

public enum ProjectItemType
{
    Folder,
    File
}

public class ProjectItemNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public ProjectItemType Type { get; set; }
    public List<ProjectItemNode> Children { get; set; } = new();
}