using System.Windows;
using HelpDesk_System.Services;

namespace HelpDesk_System.Windows
{
	public partial class RegisterWindow : Window
	{
		private readonly WindowNavigationService _navigationService;

		public RegisterWindow(WindowNavigationService navigationService)
		{
			InitializeComponent();
			_navigationService = navigationService;

            Width = SystemParameters.WorkArea.Width * 0.8;
            Height = SystemParameters.WorkArea.Height * 0.8;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

		private void OpenLoginButton_Click(object sender, RoutedEventArgs e)
		{
			_navigationService.OpenLogin(this);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
