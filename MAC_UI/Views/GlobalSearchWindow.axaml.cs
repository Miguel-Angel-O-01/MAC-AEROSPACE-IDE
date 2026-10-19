using System;
using Avalonia.Controls;
using Avalonia.Input;
using MAC_UI.Models;
using MAC_UI.ViewModels;

namespace MAC_UI.Views;

public partial class GlobalSearchWindow : Window
{
    public GlobalSearchWindow()
    {
        InitializeComponent();

        Opened += (s, e) => SearchInputBox.Focus();

        DataContextChanged += (s, e) =>
        {
            if (DataContext is GlobalSearchViewModel vm)
            {
                vm.RequestClose += (item) => Close(item);
            }
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close(null);
            }
            else if (e.Key == Key.Enter && DataContext is GlobalSearchViewModel vm)
            {
                vm.ConfirmSelection(vm.SelectedItem);
            }
        };

        ResultsListBox.DoubleTapped += (s, e) =>
        {
            if (DataContext is GlobalSearchViewModel vm)
            {
                vm.ConfirmSelection(vm.SelectedItem);
            }
        };
    }
}