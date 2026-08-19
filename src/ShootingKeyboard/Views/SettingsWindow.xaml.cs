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

    private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Profile Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Import Profile"
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ImportProfileFromFile(dialog.FileName);
        }
    }

    private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _viewModel.SelectedProfile;
        if (selected == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON Profile Files (*.json)|*.json",
            FileName = $"{selected.Name}.json",
            Title = "Export Profile"
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportSelectedProfileToFile(dialog.FileName);
        }
    }
}
