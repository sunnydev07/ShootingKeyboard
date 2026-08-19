using System.Windows;
using ShootingKeyboard.ViewModels;

namespace ShootingKeyboard.Views;

public partial class SoundPackWindow : Window
{
    private readonly SoundPackViewModel _viewModel;

    public SoundPackWindow(SoundPackViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InstallPackZip_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Zip Archive (*.zip)|*.zip|All Files (*.*)|*.*",
            Title = "Install Sound Pack from ZIP"
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.InstallPackZip(dialog.FileName);
        }
    }

    private void ExportPackZip_Click(object sender, RoutedEventArgs e)
    {
        var selected = _viewModel.SelectedPack;
        if (selected == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Zip Archive (*.zip)|*.zip",
            FileName = $"{selected.Id}.zip",
            Title = "Export Sound Pack to ZIP"
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportSelectedPackToZip(dialog.FileName);
        }
    }
}
