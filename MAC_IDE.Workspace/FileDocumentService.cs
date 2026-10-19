using System.IO;
using System.Threading.Tasks;
using MAC_IDE.Core.Interfaces;

namespace MAC_IDE.Workspace;

// DEBE TENER 'public'
public class FileDocumentService : IDocumentService
{
    public string? CurrentFilePath { get; private set; }
    public bool IsDirty { get; set; }

    public async Task<string> LoadDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Archivo no encontrado", filePath);

        CurrentFilePath = filePath;
        IsDirty = false;
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task SaveDocumentAsync(string filePath, string content)
    {
        await File.WriteAllTextAsync(filePath, content);
        CurrentFilePath = filePath;
        IsDirty = false;
    }
}