using System.Windows;
using ShootingKeyboard.ViewModels;

namespace ShootingKeyboard.Views;

public partial class DiagnosticsWindow : Window
{
    private readonly DiagnosticsViewModel _viewModel;

    public DiagnosticsWindow(DiagnosticsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
