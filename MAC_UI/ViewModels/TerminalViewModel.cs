using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;

namespace MAC_UI.ViewModels;

public sealed record ShellProfile(string Name, string Path, string[] Args);

public partial class TerminalViewModel : ObservableObject, IRecipient<ActiveProjectChangedMessage>
{
    [ObservableProperty] private string _title = "Terminal";
    [ObservableProperty] private string _workingDirectory;
    [ObservableProperty] private ShellProfile? _selectedProfile;

    public ObservableCollection<ShellProfile> Profiles { get; }

    /// <summary>La vista escucha esto para (re)lanzar el shell.</summary>
    public event Action<bool>? RelaunchRequested;

    public TerminalViewModel()
    {
        Profiles = new ObservableCollection<ShellProfile>(DetectProfiles());
        _selectedProfile = Profiles.FirstOrDefault();
        _workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        WeakReferenceMessenger.Default.Register(this);
    }

    partial void OnSelectedProfileChanged(ShellProfile? value)
    {
        if (value != null) RelaunchRequested?.Invoke(false);
    }

    public void Receive(ActiveProjectChangedMessage message)
    {
        var dir = message.ProjectDirectory;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        if (SamePath(dir, WorkingDirectory)) return; // el mensaje llega duplicado
        WorkingDirectory = dir;
        RelaunchRequested?.Invoke(false);
    }

    [RelayCommand]
    private void Restart() => RelaunchRequested?.Invoke(true);

    private static bool SamePath(string a, string b) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static List<ShellProfile> DetectProfiles()
    {
        var list = new List<ShellProfile>();

        if (OperatingSystem.IsWindows())
        {
            var pwsh = FindOnPath("pwsh.exe") ?? FirstExisting(@"C:\Program Files\PowerShell\7\pwsh.exe");
            if (pwsh != null) list.Add(new ShellProfile("PowerShell 7", pwsh, new[] { "-NoLogo" }));

            var winPs = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
            if (File.Exists(winPs)) list.Add(new ShellProfile("Windows PowerShell", winPs, new[] { "-NoLogo" }));

            var cmd = Environment.GetEnvironmentVariable("ComSpec");
            if (!string.IsNullOrEmpty(cmd) && File.Exists(cmd))
                list.Add(new ShellProfile("Símbolo del sistema", cmd, Array.Empty<string>()));

            var bash = FirstExisting(@"C:\Program Files\Git\bin\bash.exe");
            if (bash != null) list.Add(new ShellProfile("Git Bash", bash, new[] { "--login", "-i" }));
        }
        else
        {
            var sh = Environment.GetEnvironmentVariable("SHELL");
            list.Add(new ShellProfile("Shell", !string.IsNullOrEmpty(sh) && File.Exists(sh) ? sh : "/bin/bash",
                Array.Empty<string>()));
        }

        return list;
    }

    private static string? FirstExisting(string path) => File.Exists(path) ? path : null;

    private static string? FindOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var full = Path.Combine(dir.Trim().Trim('"'), exe);
                if (File.Exists(full)) return full;
            }
            catch { /* entrada de PATH inválida */ }
        }
        return null;
    }
}