using Avalonia.Controls;
using inFAMOUSReborn.Launcher.ViewModels;
using System.ComponentModel;

namespace inFAMOUSReborn.Launcher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.TerminalOutput))
        {
            var scrollViewer = this.FindControl<ScrollViewer>("TerminalScroll");
            if (scrollViewer != null)
            {
                scrollViewer.Offset = new Avalonia.Vector(scrollViewer.Offset.X, double.MaxValue);
            }
        }
    }
}