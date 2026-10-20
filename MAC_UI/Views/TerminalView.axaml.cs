using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Iciclecreek.Terminal;
using MAC_IDE.Core;
using MAC_UI.ViewModels;

namespace MAC_UI.Views;

public partial class TerminalView : UserControl
{
    private TerminalViewModel? _vm;
    private TerminalControl? _terminal;
    private string? _launchedDir;
    private ShellProfile? _launchedProfile;
    private bool _exitHooked;

    public TerminalView()
    {
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine($"[Terminal] vista creada #{GetHashCode()}");
        Loaded += (_, _) => OnAttached();
        Unloaded += (_, _) => Detach();
    }

    private void OnAttached()
    {
        Detach();
        _vm = DataContext as TerminalViewModel;
        if (_vm == null) return;

        _vm.RelaunchRequested += OnRelaunchRequested;

        if (!_exitHooked &&
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) => StopShell();
            _exitHooked = true;
        }

        EnsureShell();
    }

    private void Detach()
    {
        if (_vm != null) _vm.RelaunchRequested -= OnRelaunchRequested;
        _vm = null;
    }

    private void OnRelaunchRequested(bool force) =>
        Dispatcher.UIThread.Post(() => EnsureShell(force));

    private static TerminalControl CreateTerminal() => new()
    {
        Background = Brush.Parse("#1E1E1E"),
        Foreground = Brush.Parse("#CCCCCC"),
        FontFamily = new FontFamily("Cascadia Mono, Consolas"),
        FontSize = 13
    };

    private void EnsureShell(bool force = false)
    {
        var vm = _vm;
        if (vm == null) return;

        var profile = vm.SelectedProfile;
        if (profile == null)
        {
            if (vm.Profiles.Count == 0) Log("[Terminal] No se encontró ningún shell instalado.");
            return; // el ComboBox puede enviar null un instante: se ignora
        }

        var dir = Directory.Exists(vm.WorkingDirectory)
            ? vm.WorkingDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        bool changed = _terminal == null
                       || !string.Equals(dir, _launchedDir, StringComparison.OrdinalIgnoreCase)
                       || !ReferenceEquals(profile, _launchedProfile);

        if (!force && !changed) return; // volver a la pestaña no reinicia nada

        StopShell();

        var terminal = CreateTerminal(); // control nuevo = vista limpia
        TerminalContainer.Child = terminal;
        _terminal = terminal;
        _launchedDir = dir;
        _launchedProfile = profile;

        try
        {
            terminal.LaunchProcess(dir, profile.Path, profile.Args);
        }
        catch (Exception ex)
        {
            Log($"[Terminal] No se pudo iniciar {profile.Name}: {ex.Message}");
        }
    }

    private void StopShell()
    {
        try { _terminal?.Kill(); } catch { /* ya había terminado */ }
    }

    private static void Log(string text) =>
        WeakReferenceMessenger.Default.Send(new OutputLogMessage("Terminal", text));
}