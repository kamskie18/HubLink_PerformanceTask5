using Avalonia.Controls;
using Avalonia.Interactivity;
using HubLink.Desktop.ViewModels;

namespace HubLink.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.FetchShipmentsCommand.ExecuteAsync(null);
        }
    }
}
