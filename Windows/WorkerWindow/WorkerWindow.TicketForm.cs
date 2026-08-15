using System.Windows;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class WorkerWindow
{
    private async void SubmitTicketButton_Click(object sender, RoutedEventArgs e)
    {
        ClearFormMessages();

        var title = TitleTextBox.Text.Trim();
        var description = DescriptionTextBox.Text.Trim();

        if (!TryGetTicketFormValues(
                title,
                description,
                out var problemType,
                out var workImpact,
                out var affectedPeople))
        {
            return;
        }

        SetTicketFormEnabled(false);

        try
        {
            await _ticketService.CreateTicketAsync(
                _currentUser.Id,
                title,
                description,
                problemType,
                workImpact,
                affectedPeople);

            ResetTicketForm();
            ShowFormSuccessMessage("Ticket was submitted successfully.");
            await LoadUserTicketsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowFormErrorMessage(
                "The ticket could not be sent. Check the database connection and try again.");
        }
        finally
        {
            SetTicketFormEnabled(true);
        }
    }

    private bool TryGetTicketFormValues(
        string title,
        string description,
        out ProblemType problemType,
        out WorkImpact workImpact,
        out AffectedPeople affectedPeople)
    {
        var isValid = true;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowMessage(TitleErrorText, "Enter a ticket title.");
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ShowMessage(DescriptionErrorText, "Describe the issue.");
            isValid = false;
        }

        if (ProblemTypeComboBox.SelectedItem is ProblemType selectedProblemType)
        {
            problemType = selectedProblemType;
        }
        else
        {
            problemType = default;
            ShowMessage(ProblemTypeErrorText, "Select a problem type.");
            isValid = false;
        }

        if (WorkImpactComboBox.SelectedItem is WorkImpact selectedWorkImpact)
        {
            workImpact = selectedWorkImpact;
        }
        else
        {
            workImpact = default;
            ShowMessage(WorkImpactErrorText, "Select the work impact.");
            isValid = false;
        }

        if (AffectedPeopleComboBox.SelectedItem is AffectedPeople selectedAffectedPeople)
        {
            affectedPeople = selectedAffectedPeople;
        }
        else
        {
            affectedPeople = default;
            ShowMessage(AffectedPeopleErrorText, "Select who is affected.");
            isValid = false;
        }

        return isValid;
    }

    private void ResetTicketForm()
    {
        TitleTextBox.Clear();
        DescriptionTextBox.Clear();
        ProblemTypeComboBox.SelectedIndex = -1;
        WorkImpactComboBox.SelectedIndex = -1;
        AffectedPeopleComboBox.SelectedIndex = -1;
    }

    private void SetTicketFormEnabled(bool isEnabled)
    {
        TitleTextBox.IsEnabled = isEnabled;
        DescriptionTextBox.IsEnabled = isEnabled;
        ProblemTypeComboBox.IsEnabled = isEnabled;
        WorkImpactComboBox.IsEnabled = isEnabled;
        AffectedPeopleComboBox.IsEnabled = isEnabled;
        SubmitTicketButton.IsEnabled = isEnabled;
    }

    private void ClearFormMessages()
    {
        HideMessage(TitleErrorText);
        HideMessage(DescriptionErrorText);
        HideMessage(ProblemTypeErrorText);
        HideMessage(WorkImpactErrorText);
        HideMessage(AffectedPeopleErrorText);
        HideMessage(FormSuccessMessageText);
        HideMessage(FormErrorMessageText);
    }
}
