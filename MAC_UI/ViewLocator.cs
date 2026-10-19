using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Core;
using MAC_UI.ViewModels;

namespace MAC_UI;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param == null) return null;

        // 1. Si Dock.Avalonia envía un Tool o Document, extraer su ViewModel interno
        if (param is IDockable dockable)
        {
            param = dockable.Context;
            if (param == null) return null;
        }

        // 2. Mapeos directos rápidos para evitar fallos de reflexión
        if (param is SolutionExplorerViewModel)
            return new Views.SolutionExplorerView { DataContext = param };

        if (param is CodeEditorViewModel)
            return new Views.CodeEditorView { DataContext = param };

        if (param is MainToolbarViewModel)
            return new Views.MainToolbarView { DataContext = param };

        if (param is TopMenuBarViewModel)
            return new Views.TopMenuBarView { DataContext = param };

        if (param is ModuleTabsBarViewModel)
            return new Views.ModuleTabsBarView { DataContext = param };

        if (param is StatusBarViewModel)
            return new Views.StatusBarView { DataContext = param };
        if (param is TerminalViewModel)
            return new Views.TerminalView { DataContext = param };
        if (param is OutputViewModel)
            return new Views.OutputView { DataContext = param };

        // 3. Resolución por convención de nombres estándar de Avalonia
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = param;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase ||
               data is ObservableObject ||
               data is IDockable;
    }
}