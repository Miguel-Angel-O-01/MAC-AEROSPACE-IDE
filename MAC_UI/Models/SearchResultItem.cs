namespace MAC_UI.Models;

public enum SearchResultKind
{
    Code,
    File,
    Type,
    Member,
    IdeCommand
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string LineInfo { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string FullFilePath { get; set; } = string.Empty;
    public SearchResultKind Kind { get; set; }
}