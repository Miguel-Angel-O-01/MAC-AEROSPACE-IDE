using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace MAC_UI.ViewModels;

public partial class ModuleTabsBarViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedTabIndex = 1; // 1 = Code por defecto

    public ObservableCollection<string> Modules { get; } = new()
    {
        "Inicio",
        "Code",
        "LM Studio",
        "MCP",
        "AI Team"
    };

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value >= 0 && value < Modules.Count)
        {
            string moduleName = Modules[value];
            WeakReferenceMessenger.Default.Send(new StatusUpdateMessage($"Cambiando al entorno: {moduleName}", ResetDelayMs: 2000));
        }
    }
}