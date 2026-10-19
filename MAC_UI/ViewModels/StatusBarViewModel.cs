using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace MAC_UI.ViewModels;

// Mensaje desacoplado para pedir cambios en la barra de estado
public record StatusUpdateMessage(string Message, string? Color = null, int ResetDelayMs = 0);

public partial class StatusBarViewModel : ObservableObject, IRecipient<StatusUpdateMessage>
{
    private CancellationTokenSource? _resetCts;

    [ObservableProperty]
    private string _statusMessage = "Listo";

    [ObservableProperty]
    private string _statusBarColor = "#007ACC";

    [ObservableProperty]
    private string _gitBranch = "main";

    [ObservableProperty]
    private string _currentLanguage = "C#";

    public StatusBarViewModel()
    {
        // Suscribirse a mensajes sin acoplarse a ninguna ventana
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(StatusUpdateMessage message)
    {
        _resetCts?.Cancel();
        _resetCts = new CancellationTokenSource();
        var token = _resetCts.Token;

        StatusMessage = message.Message;

        if (!string.IsNullOrEmpty(message.Color))
        {
            StatusBarColor = message.Color;
        }

        if (message.ResetDelayMs > 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(message.ResetDelayMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            StatusMessage = "Listo";
                            StatusBarColor = "#007ACC";
                        });
                    }
                }
                catch (TaskCanceledException) { }
            }, token);
        }
    }
}