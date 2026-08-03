using System.Windows;
using HelpDesk_System.Services;

namespace HelpDesk_System.Windows
{
	public partial class LoginWindow : Window
	{
		private readonly AuthService _authService;
		private readonly WindowNavigationService _navigationService;

		public LoginWindow(AuthService authService, WindowNavigationService navigationService)
		{
			InitializeComponent();
			_authService = authService;
			_navigationService = navigationService;

            Width = SystemParameters.WorkArea.Width * 0.8;
            Height = SystemParameters.WorkArea.Height * 0.8;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            EmailErrorText.Visibility = Visibility.Collapsed;
            PasswordErrorText.Visibility = Visibility.Collapsed;

            var email = EmailTextBox.Text.Trim();
            var password = PasswordInput.Password;
            var hasError = false;

            if (string.IsNullOrWhiteSpace(email))
            {
                EmailErrorText.Text = "Enter your email.";
                EmailErrorText.Visibility = Visibility.Visible;
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                PasswordErrorText.Text = "Enter your password.";
                PasswordErrorText.Visibility = Visibility.Visible;
                hasError = true;
            }

            if (hasError)
            {
                return;
            }

            var user = await _authService.LoginAsync(email, password);

            if (user == null)
            {
                PasswordErrorText.Text = "Incorrect email or password.";
                PasswordErrorText.Visibility = Visibility.Visible;
                return;
            }

			_navigationService.OpenWorkspace(this, user);
		}

		private void OpenRegisterButton_Click(object sender, RoutedEventArgs e)
		{
			_navigationService.OpenRegister(this);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
