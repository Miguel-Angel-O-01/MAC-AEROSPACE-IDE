using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;

namespace MAC_UI.ViewModels;

public partial class OutputViewModel : ObservableObject, IRecipient<OutputLogMessage>
{
    // Diccionario en memoria para retener los logs separados por canal
    private readonly Dictionary<string, System.Text.StringBuilder> _channelBuffers = new();

    [ObservableProperty]
    private string _selectedChannel = "Compilación";

    [ObservableProperty]
    private string _currentChannelText = string.Empty;

    public ObservableCollection<string> OutputChannels { get; } = new()
    {
        "Compilación",
        "Depuración",
        "Control de código fuente (Git)"
    };

    public OutputViewModel()
    {
        foreach (var ch in OutputChannels)
        {
            _channelBuffers[ch] = new System.Text.StringBuilder();
        }

        WeakReferenceMessenger.Default.Register(this);

        AppendLog("Compilación", "=== MAC AEROESPACE Studio listo ===\n");
    }

    partial void OnSelectedChannelChanged(string value)
    {
        RefreshViewText();
    }

    public void Receive(OutputLogMessage message)
    {
        AppendLog(message.Channel, message.Text);
    }

    public void AppendLog(string channel, string text)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!_channelBuffers.ContainsKey(channel))
            {
                _channelBuffers[channel] = new System.Text.StringBuilder();
                OutputChannels.Add(channel);
            }

            _channelBuffers[channel].AppendLine(text);

            if (SelectedChannel == channel)
            {
                RefreshViewText();
            }
        });
    }

    private void RefreshViewText()
    {
        if (_channelBuffers.TryGetValue(SelectedChannel, out var sb))
        {
            CurrentChannelText = sb.ToString();
        }
        else
        {
            CurrentChannelText = string.Empty;
        }
    }

    [RelayCommand]
    public void ClearOutput()
    {
        if (_channelBuffers.TryGetValue(SelectedChannel, out var sb))
        {
            sb.Clear();
            CurrentChannelText = string.Empty;
        }
    }
}