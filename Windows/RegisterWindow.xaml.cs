using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Services;

namespace HelpDesk_System.Windows
{
    public partial class RegisterWindow : Window
    {
        private const int MinimumNameLength = 2;
        private const int MaximumNameLength = 40;
        private const int MaximumEmailLength = 80;
        private const int MinimumPasswordLength = 8;
        private const int MaximumPasswordLength = 64;
        private static readonly TimeSpan SuccessMessageDisplayDuration = TimeSpan.FromSeconds(2);

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
            ClearFormMessages();

            var firstName = FirstNameTextBox.Text.Trim();
            var lastName = LastNameTextBox.Text.Trim();
            var email = EmailTextBox.Text.Trim();
            var password = PasswordInput.Password;
            var confirmPassword = ConfirmPasswordInput.Password;

            if (!ValidateForm(firstName, lastName, email, password, confirmPassword))
            {
                return;
            }

            var requestedRole = GetRequestedRole();

            SetFormActionsEnabled(false);

            bool requestSubmitted;

            try
            {
                requestSubmitted = await _registrationRequestService.SubmitAsync(
                    firstName,
                    lastName,
                    email,
                    password,
                    requestedRole);
            }
            catch
            {
                ShowFieldError(
                    FormErrorTextBlock,
                    "The registration request could not be sent. Please try again.");

                SetFormActionsEnabled(true);
                return;
            }

            if (!requestSubmitted)
            {
                ShowFieldError(
                    EmailErrorTextBlock,
                    "This email is already registered or has a pending request.");

                SetFormActionsEnabled(true);
                return;
            }

            ShowSuccessMessage(
                "Registration request sent successfully. Redirecting to login...");

            await Task.Delay(SuccessMessageDisplayDuration);
            _navigationService.OpenLogin(this);
        }

        private bool ValidateForm(
            string firstName,
            string lastName,
            string email,
            string password,
            string confirmPassword)
        {
            var isValid = true;

            var firstNameError = ValidateName(firstName, "first name");
            if (firstNameError is not null)
            {
                ShowFieldError(FirstNameErrorTextBlock, firstNameError);
                isValid = false;
            }

            var lastNameError = ValidateName(lastName, "last name");
            if (lastNameError is not null)
            {
                ShowFieldError(LastNameErrorTextBlock, lastNameError);
                isValid = false;
            }

            var emailError = ValidateEmail(email);
            if (emailError is not null)
            {
                ShowFieldError(EmailErrorTextBlock, emailError);
                isValid = false;
            }

            var passwordError = ValidatePassword(password);
            if (passwordError is not null)
            {
                ShowFieldError(PasswordErrorTextBlock, passwordError);
                isValid = false;
            }

            var confirmPasswordError = ValidateConfirmedPassword(password, confirmPassword);
            if (confirmPasswordError is not null)
            {
                ShowFieldError(ConfirmPasswordErrorTextBlock, confirmPasswordError);
                isValid = false;
            }

            if (RoleComboBox.SelectedIndex is not 0 and not 1)
            {
                ShowFieldError(RoleErrorTextBlock, "Select a requested role.");
                isValid = false;
            }

            return isValid;
        }

        private static string? ValidateName(string name, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return $"Enter your {fieldName}.";
            }

            if (name.Length < MinimumNameLength)
            {
                return $"The {fieldName} must contain at least {MinimumNameLength} letters.";
            }

            if (name.Length > MaximumNameLength)
            {
                return $"The {fieldName} cannot exceed {MaximumNameLength} characters.";
            }

            if (!char.IsLetter(name[0]) ||
                !char.IsLetter(name[^1]) ||
                name.Any(character =>
                    !char.IsLetter(character) && !IsNameSeparator(character)))
            {
                return $"The {fieldName} can contain letters, spaces, hyphens and apostrophes.";
            }

            if (name.Zip(name.Skip(1), (first, second) =>
                    IsNameSeparator(first) && IsNameSeparator(second))
                .Any(hasAdjacentSeparators => hasAdjacentSeparators))
            {
                return $"The {fieldName} cannot contain repeated separators.";
            }

            return null;
        }

        private static bool IsNameSeparator(char character)
        {
            return character is ' ' or '-' or '\'';
        }

        private static string? ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Enter your email.";
            }

            if (email.Length > MaximumEmailLength)
            {
                return $"The email cannot exceed {MaximumEmailLength} characters.";
            }

            var atIndex = email.IndexOf('@');
            var hasSingleAt = atIndex > 0 &&
                              atIndex == email.LastIndexOf('@') &&
                              atIndex < email.Length - 1;

            if (!hasSingleAt || email.Any(char.IsWhiteSpace) || email.Contains(".."))
            {
                return "Enter a valid email address, for example name@example.com.";
            }

            var domain = email[(atIndex + 1)..];
            var lastDotIndex = domain.LastIndexOf('.');
            var hasValidDomain = lastDotIndex > 0 &&
                                 lastDotIndex < domain.Length - 2;
            var hasValidFormat = MailAddress.TryCreate(email, out var parsedEmail) &&
                                 string.Equals(
                                     parsedEmail.Address,
                                     email,
                                     StringComparison.OrdinalIgnoreCase);

            return hasValidDomain && hasValidFormat
                ? null
                : "Enter a valid email address, for example name@example.com.";
        }

        private static string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return "Enter a password.";
            }

            if (password.Length > MaximumPasswordLength)
            {
                return $"The password cannot exceed {MaximumPasswordLength} characters.";
            }

            var hasMinimumLength = password.Length >= MinimumPasswordLength;
            var hasUppercaseLetter = password.Any(char.IsUpper);
            var hasDigit = password.Any(char.IsDigit);

            return hasMinimumLength && hasUppercaseLetter && hasDigit
                ? null
                : "Use at least 8 characters, one uppercase letter and one digit.";
        }

        private static string? ValidateConfirmedPassword(string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                return "Confirm your password.";
            }

            return password == confirmPassword
                ? null
                : "Passwords do not match.";
        }

        private UserRole GetRequestedRole()
        {
            return RoleComboBox.SelectedIndex == 1
                ? UserRole.Admin
                : UserRole.Worker;
        }

        private void ClearFormMessages()
        {
            FirstNameErrorTextBlock.Visibility = Visibility.Collapsed;
            LastNameErrorTextBlock.Visibility = Visibility.Collapsed;
            EmailErrorTextBlock.Visibility = Visibility.Collapsed;
            PasswordErrorTextBlock.Visibility = Visibility.Collapsed;
            ConfirmPasswordErrorTextBlock.Visibility = Visibility.Collapsed;
            RoleErrorTextBlock.Visibility = Visibility.Collapsed;
            FormErrorTextBlock.Visibility = Visibility.Collapsed;
            FormSuccessTextBlock.Visibility = Visibility.Collapsed;
        }

        private static void ShowFieldError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowSuccessMessage(string message)
        {
            FormSuccessTextBlock.Text = message;
            FormSuccessTextBlock.Visibility = Visibility.Visible;
        }

        private void SetFormActionsEnabled(bool isEnabled)
        {
            RegisterButton.IsEnabled = isEnabled;
            OpenLoginButton.IsEnabled = isEnabled;
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
