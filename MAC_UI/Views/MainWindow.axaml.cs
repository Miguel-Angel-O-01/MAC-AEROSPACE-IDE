using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using MAC_UI.ViewModels;

namespace MAC_UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer?.TryGetFiles();
        if (files != null)
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var files = e.DataTransfer?.TryGetFiles();
            if (files == null) return;

            foreach (var item in files)
            {
                string localPath = item.Path.LocalPath;

                if (Directory.Exists(localPath))
                {
                    vm.SetActiveWorkspace(localPath);
                }
                else if (File.Exists(localPath))
                {
                    _ = vm.OpenDocumentTabAsync(localPath);
                }
            }
        }
        e.Handled = true;
    }
}