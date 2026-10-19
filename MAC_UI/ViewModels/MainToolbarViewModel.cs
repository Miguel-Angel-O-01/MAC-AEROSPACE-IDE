using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MAC_UI.ViewModels;

public partial class MainToolbarViewModel : ObservableObject, IRecipient<ActiveProjectChangedMessage>
{
    private string _activeProjectDirectory = string.Empty;

    [ObservableProperty]
    private string _selectedConfiguration = "Debug";

    [ObservableProperty]
    private string _selectedPlatform = "Any CPU";

    [ObservableProperty]
    private string _activeStartupProject = "MAC_UI";

    [ObservableProperty]
    private bool _isDebugging = false;

    public ObservableCollection<string> Configurations { get; } = new() { "Debug", "Release" };
    public ObservableCollection<string> Platforms { get; } = new() { "Any CPU", "x64", "ARM64" };

    public MainToolbarViewModel()
    {
        // Suscripción al bus para recibir notificaciones cuando cambie el proyecto en el Explorador
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ActiveProjectChangedMessage message)
    {
        ActiveStartupProject = message.ProjectName;
        _activeProjectDirectory = message.ProjectDirectory;
    }

    public void SetWorkingDirectory(string folderPath)
    {
        _activeProjectDirectory = folderPath;
        ActiveStartupProject = Path.GetFileName(folderPath.TrimEnd('\\', '/'));
    }

    [RelayCommand]
    public async Task RunProjectAsync()
    {
        string targetDir = _activeProjectDirectory;

        // 1. Validar que exista una carpeta abierta
        if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
        {
            WeakReferenceMessenger.Default.Send(new OutputLogMessage("Compilación",
                "[Aviso] No hay un proyecto o solución abierto para compilar. Abre una carpeta primero."));
            return;
        }

        // 2. Notificar que queremos ver el panel de salida (Capturado por MainWindowViewModel -> DockFactory)
        WeakReferenceMessenger.Default.Send(new ShowOutputMessage());

        // Limpiar la barra invertida para MSBuild en Windows
        targetDir = targetDir.TrimEnd('\\', '/');

        // Buscar solución en el directorio
        string projectOrSln = Directory.GetFiles(targetDir, "*.slnx").FirstOrDefault()
                              ?? Directory.GetFiles(targetDir, "*.sln").FirstOrDefault()
                              ?? Directory.GetFiles(targetDir, "*.csproj").FirstOrDefault()
                              ?? targetDir;

        WeakReferenceMessenger.Default.Send(new OutputLogMessage("Compilación",
            $"\n------ Compilación iniciada: Proyecto: {ActiveStartupProject}, Configuración: {SelectedConfiguration} {SelectedPlatform} ------\nDirectorio: {projectOrSln}"));

        // 3. Ejecutar dotnet build real en 2º plano
        await Task.Run(() =>
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectOrSln}\" -c {SelectedConfiguration}",
                    WorkingDirectory = targetDir,
                    StandardOutputEncoding = System.Text.Encoding.UTF8, // Evita caracteres raros
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new System.Diagnostics.Process { StartInfo = psi };

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        WeakReferenceMessenger.Default.Send(new OutputLogMessage("Compilación", e.Data));
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        WeakReferenceMessenger.Default.Send(new OutputLogMessage("Compilación", $"[Error] {e.Data}"));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                string balance = process.ExitCode == 0
                    ? "\n========== Compilación: Correcto, 0 errores =========="
                    : $"\n========== Compilación: Falló (Código de salida {process.ExitCode}) ==========";

                WeakReferenceMessenger.Default.Send(new OutputLogMessage("Compilación", balance));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new OutputLogMessage("Compilación", $"Error al invocar el compilador: {ex.Message}"));
            }
        });
    }

    [RelayCommand]
    public async Task BuildSolutionAsync()
    {
        await RunProjectAsync();
    }

    [RelayCommand]
    public void Undo() { }

    [RelayCommand]
    public void Redo() { }
}