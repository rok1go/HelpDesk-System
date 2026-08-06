using System.Windows;
using System.Windows.Media;
using HelpDesk_System.Services;
using HelpDesk_System.Models.Enums;

namespace HelpDesk_System.Windows
{
    public partial class RegisterWindow : Window
    {
        private readonly WindowNavigationService _navigationService;
        private readonly RegistrationRequestService _registrationRequestService;

        public RegisterWindow(
            RegistrationRequestService registrationRequestService,
            WindowNavigationService navigationService)
        {
            InitializeComponent();

            _registrationRequestService = registrationRequestService;
            _navigationService = navigationService;

            Width = SystemParameters.WorkArea.Width * 0.8;
            Height = SystemParameters.WorkArea.Height * 0.8;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;


        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationStatusTextBlock.Visibility = Visibility.Collapsed;

            var firstName = FirstNameTextBox.Text.Trim();
            var lastName = LastNameTextBox.Text.Trim();
            var email = EmailTextBox.Text.Trim();
            var password = PasswordInput.Password;
            var confirmPassword = ConfirmPasswordInput.Password;

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowError("Fill in all fields.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            var requestedRole = RoleComboBox.SelectedIndex == 1
                ? UserRole.Admin
                : UserRole.Worker;

            RegisterButton.IsEnabled = false;

            try
            {
                var requestSubmitted = await _registrationRequestService.SubmitAsync(
                    firstName,
                    lastName,
                    email,
                    password,
                    requestedRole);

                if (!requestSubmitted)
                {
                    ShowError("This email is already registered or has a pending request.");
                    return;
                }

                ShowSuccess("Your registration request has been sent. Wait for administrator approval.");

                PasswordInput.Clear();
                ConfirmPasswordInput.Clear();
            }
            catch
            {
                ShowError("The registration request could not be sent. Please try again.");
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            RegistrationStatusTextBlock.Text = message;
            RegistrationStatusTextBlock.Foreground = Brushes.Firebrick;
            RegistrationStatusTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowSuccess(string message)
        {
            RegistrationStatusTextBlock.Text = message;
            RegistrationStatusTextBlock.Foreground = Brushes.SeaGreen;
            RegistrationStatusTextBlock.Visibility = Visibility.Visible;
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