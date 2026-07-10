using System.Windows;

namespace HelpDesk_System.Windows;

public partial class StartWindow : Window
{
    public StartWindow()
    {
        InitializeComponent();
    }
    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginWindow window = new LoginWindow();
        window.Show();

        Close();
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        RegisterWindow window = new RegisterWindow();
        window.Show();

        Close();
    }
}