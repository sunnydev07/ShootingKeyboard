using System.Windows;
using ShootingKeyboard.ViewModels;

namespace ShootingKeyboard.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += Close;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RevertRuntimeChanges();
        Close();
    }
}
