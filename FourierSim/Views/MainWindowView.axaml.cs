using Avalonia.Controls;

namespace FourierSim.Views;

public partial class MainWindowView : Window
{
    public MainWindowView()
    {
        InitializeComponent();

        Opened += (_, _) =>
        {
            WindowState = WindowState.Maximized;
        };
    }
    
}