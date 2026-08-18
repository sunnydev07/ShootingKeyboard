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
}
