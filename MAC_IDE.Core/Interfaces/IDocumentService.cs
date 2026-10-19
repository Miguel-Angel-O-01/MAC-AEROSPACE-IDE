namespace MAC_IDE.Core.Interfaces;

public interface IDocumentService
{
    string? CurrentFilePath { get; }
    bool IsDirty { get; set; }

    Task<string> LoadDocumentAsync(string filePath);
    Task SaveDocumentAsync(string filePath, string content);
}