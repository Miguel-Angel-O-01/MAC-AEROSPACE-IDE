using Avalonia.Controls;
using MAC_UI.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;

namespace MAC_UI.Views;

public partial class TerminalView : UserControl, IRecipient<ChangeTerminalDirectoryMessage>
{
    private bool _isTerminalStarted = false;

    public TerminalView()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);

        this.Loaded += (s, e) =>
        {
            if (_isTerminalStarted) return;

            if (DataContext is TerminalViewModel vm)
            {
                string shell = System.OperatingSystem.IsWindows()
                    ? @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
                    : "/bin/bash";

                string startDir = string.IsNullOrEmpty(vm.WorkingDirectory)
                    ? System.Environment.CurrentDirectory
                    : vm.WorkingDirectory;

                TerminalHost.LaunchProcess(startDir, shell);
                _isTerminalStarted = true;
            }
        };
    }

    public void Receive(ChangeTerminalDirectoryMessage message)
    {
        if (_isTerminalStarted && TerminalHost != null)
        {
            // Construir el comando de cambio de directorio (con comillas por si hay espacios)
            string cdCommand = $"cd \"{message.NewDirectory}\"\r\n";

            // Enviar el comando al proceso de la terminal
            // Revisa la API de TerminalHost. Usualmente es algo como Write(), SendText(), o acceder al Process.StandardInput
            // Ejemplo hipotético (depende de la API de Iciclecreek):
            // TerminalHost.Write(cdCommand); 
        }
    }
}