using System.Windows;
using System.Windows.Controls;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class AdminWindow
{
    private async Task LoadRegistrationRequestsAsync()
    {
        var selectedRequestId = _selectedRegistrationRequest?.Id;

        try
        {
            _pendingRegistrationRequests = await _registrationRequestService.GetPendingRequestsAsync();
            RegistrationRequestsList.ItemsSource = _pendingRegistrationRequests;

            RegistrationRequestsCountText.Text = DisplayFormatter.FormatCount(
                _pendingRegistrationRequests.Count,
                "pending request",
                "pending requests");

            RegistrationRequestsButton.Content =
                $"Registration requests ({_pendingRegistrationRequests.Count})";

            RegistrationRequestsEmptyText.Visibility = _pendingRegistrationRequests.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!selectedRequestId.HasValue)
            {
                return;
            }

            var currentRequest = _pendingRegistrationRequests
                .FirstOrDefault(request => request.Id == selectedRequestId.Value);

            if (currentRequest is null)
            {
                HideRegistrationRequestDetails();
            }
            else
            {
                ShowRegistrationRequestDetails(currentRequest);
            }
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                RegistrationStatusMessageText,
                "Registration requests could not be loaded. Check the database connection.");
        }
    }

    private void RegistrationRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
        RegistrationRequestsPanel.Visibility = Visibility.Visible;
    }

    private void RegistrationRequestCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RegistrationRequest request })
        {
            ShowRegistrationRequestDetails(request);
        }
    }

    private void ShowRegistrationRequestDetails(RegistrationRequest request)
    {
        _selectedRegistrationRequest = request;

        HideMessage(RegistrationStatusMessageText);

        RegistrationApplicantNameText.Text = $"{request.FirstName} {request.LastName}".Trim();
        RegistrationApplicantEmailText.Text = request.Email;
        RegistrationRequestedRoleText.Text = DisplayFormatter.FormatEnum(request.RequestedRole);
        RegistrationCreatedText.Text = DisplayFormatter.FormatLocalDateTime(request.CreatedAt);
        AssignedRoleComboBox.SelectedItem = request.RequestedRole;
        RegistrationDeclineReasonTextBox.Clear();

        HideMessage(RegistrationFormErrorText);
        HideMessage(RegistrationDeclineReasonErrorText);

        RegistrationSelectionHintText.Visibility = Visibility.Collapsed;
        RegistrationDetailsContent.Visibility = Visibility.Visible;
    }

    private void HideRegistrationRequestDetails()
    {
        _selectedRegistrationRequest = null;
        RegistrationDetailsContent.Visibility = Visibility.Collapsed;
        RegistrationSelectionHintText.Visibility = Visibility.Visible;

        HideMessage(RegistrationFormErrorText);
        HideMessage(RegistrationDeclineReasonErrorText);
    }

    private void HideRegistrationRequestsPanel()
    {
        RegistrationRequestsPanel.Visibility = Visibility.Collapsed;
    }

    private async void ApproveRegistrationRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRegistrationRequest is null ||
            AssignedRoleComboBox.SelectedItem is not UserRole assignedRole)
        {
            ShowMessage(
                RegistrationFormErrorText,
                "Select the role that will be assigned to the user.");

            return;
        }

        HideMessage(RegistrationFormErrorText);
        SetRegistrationActionButtonsEnabled(false);

        try
        {
            var requestWasApproved = await _registrationRequestService.ApproveAsync(
                _selectedRegistrationRequest.Id,
                _currentAdmin.Id,
                assignedRole);

            if (!requestWasApproved)
            {
                ShowMessage(
                    RegistrationFormErrorText,
                    "The request has already been processed or the email is already in use.");

                return;
            }

            HideRegistrationRequestDetails();
            ShowMessage(
                RegistrationStatusMessageText,
                "Request approved. The user can now sign in.");

            await LoadRegistrationRequestsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                RegistrationStatusMessageText,
                "The request could not be processed. Check the database connection.");
        }
        finally
        {
            SetRegistrationActionButtonsEnabled(true);
        }
    }

    private async void DeclineRegistrationRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRegistrationRequest is null)
        {
            return;
        }

        var reason = RegistrationDeclineReasonTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            ShowMessage(RegistrationDeclineReasonErrorText, "Enter a decline reason.");
            return;
        }

        HideMessage(RegistrationDeclineReasonErrorText);
        HideMessage(RegistrationFormErrorText);
        SetRegistrationActionButtonsEnabled(false);

        try
        {
            var requestWasDeclined = await _registrationRequestService.DeclineAsync(
                _selectedRegistrationRequest.Id,
                _currentAdmin.Id,
                reason);

            if (!requestWasDeclined)
            {
                ShowMessage(
                    RegistrationFormErrorText,
                    "The request is no longer pending. Refresh the list and choose another one.");

                return;
            }

            HideRegistrationRequestDetails();
            ShowMessage(RegistrationStatusMessageText, "Request declined.");
            await LoadRegistrationRequestsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                RegistrationStatusMessageText,
                "The request could not be processed. Check the database connection.");
        }
        finally
        {
            SetRegistrationActionButtonsEnabled(true);
        }
    }

    private async void RefreshRegistrationRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        HideMessage(RegistrationStatusMessageText);
        await LoadRegistrationRequestsAsync();
    }

    private void CloseRegistrationRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        HideRegistrationRequestsPanel();
    }

    private void SetRegistrationActionButtonsEnabled(bool isEnabled)
    {
        ApproveRegistrationRequestButton.IsEnabled = isEnabled;
        DeclineRegistrationRequestButton.IsEnabled = isEnabled;
        AssignedRoleComboBox.IsEnabled = isEnabled;
    }

    private void RegistrationDeclineReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            HideMessage(RegistrationDeclineReasonErrorText);
        }
    }
}
