using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;

namespace MAC_UI.ViewModels;

public partial class TerminalViewModel : ObservableObject, IRecipient<ActiveProjectChangedMessage>
{
    [ObservableProperty]
    private string _title = "Terminal";

    [ObservableProperty]
    private string _workingDirectory = System.Environment.CurrentDirectory;

    public TerminalViewModel()
    {
        // Suscribirse a los cambios de proyecto
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ActiveProjectChangedMessage message)
    {
        // Actualizar la ruta
        WorkingDirectory = message.ProjectDirectory;

        // Notificar a la vista para que ejecute el comando 'cd' en la terminal
        WeakReferenceMessenger.Default.Send(new ChangeTerminalDirectoryMessage(message.ProjectDirectory));
    }
}