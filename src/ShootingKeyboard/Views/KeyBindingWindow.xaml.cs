using System.Windows;
using ShootingKeyboard.ViewModels;

namespace ShootingKeyboard.Views;

public partial class KeyBindingWindow : Window
{
    private readonly KeyBindingViewModel _viewModel;

    public KeyBindingWindow(KeyBindingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += Close;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
