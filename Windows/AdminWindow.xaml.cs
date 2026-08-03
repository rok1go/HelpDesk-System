using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Services;

namespace HelpDesk_System.Windows;

public partial class AdminWindow : Window
{
	private readonly User _currentAdmin;
	private readonly TicketService _ticketService;
	private readonly WindowNavigationService _navigationService;
	private List<Ticket> _openTickets = [];
	private List<Ticket> _assignedTickets = [];
	private Ticket? _selectedTicket;

	public AdminWindow(User currentAdmin, TicketService ticketService, WindowNavigationService navigationService)
	{
		InitializeComponent();

		_currentAdmin = currentAdmin;
		_ticketService = ticketService;
		_navigationService = navigationService;

		Width = Math.Min(1380, SystemParameters.WorkArea.Width * 0.92);
		Height = Math.Min(820, SystemParameters.WorkArea.Height * 0.88);
		AdminNameText.Text = $"{currentAdmin.FirstName} {currentAdmin.LastName}".Trim();
		AdminEmailText.Text = currentAdmin.Email;
	}

	private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await LoadTicketsAsync();
	}

	private async Task LoadTicketsAsync()
	{
		var selectedTicketId = _selectedTicket?.Id;

		try
		{
			var openTicketsTask = _ticketService.GetOpenTicketsAsync();
			var assignedTicketsTask = _ticketService.GetAssignedTicketsAsync(_currentAdmin.Id);
			await Task.WhenAll(openTicketsTask, assignedTicketsTask);

			_openTickets = await openTicketsTask;
			_assignedTickets = await assignedTicketsTask;
			OpenTicketsList.ItemsSource = _openTickets;
			AssignedTicketsList.ItemsSource = _assignedTickets;

			OpenTicketsCountText.Text = GetTicketCountText(_openTickets.Count);
			AssignedTicketsCountText.Text = GetTicketCountText(_assignedTickets.Count);
			OpenTicketsEmptyText.Visibility = _openTickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			AssignedTicketsEmptyText.Visibility = _assignedTickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

			if (selectedTicketId.HasValue)
			{
				var currentTicket = _openTickets.Concat(_assignedTickets).FirstOrDefault(ticket => ticket.Id == selectedTicketId.Value);
				if (currentTicket is null)
				{
					HideTicketDetails();
				}
				else
				{
					ShowTicketDetails(currentTicket, false);
				}
			}
		}
		catch (Exception)
		{
			MessageBox.Show("Tickets could not be loaded. Check the database connection and make sure the latest migration is applied.", "Help Desk", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void TicketCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button { DataContext: Ticket ticket })
		{
			ShowTicketDetails(ticket);
		}
	}

	private void ShowTicketDetails(Ticket ticket, bool animate = true)
	{
		_selectedTicket = ticket;
		DetailsStatusText.Text = FormatEnum(ticket.Status);
		DetailsAuthorNameText.Text = $"{ticket.Author.FirstName} {ticket.Author.LastName}".Trim();
		DetailsAuthorEmailText.Text = ticket.Author.Email;
		DetailsTitleText.Text = ticket.Title;
		DetailsPriorityText.Text = FormatEnum(ticket.Priority);
		DetailsCreatedText.Text = ticket.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
		DetailsProblemTypeText.Text = FormatEnum(ticket.ProblemType);
		DetailsWorkImpactText.Text = FormatEnum(ticket.WorkImpact);
		DetailsAffectedPeopleText.Text = FormatEnum(ticket.AffectedPeople);
		DetailsDescriptionText.Text = ticket.Description;
		DeclineReasonTextBox.Clear();
		HideActionMessage();

		var isOpen = ticket.Status == TicketStatus.Open;
		OpenTicketActionsPanel.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
		AssignedTicketInfo.Visibility = isOpen ? Visibility.Collapsed : Visibility.Visible;
		TicketDetailsPanel.Visibility = Visibility.Visible;

		if (!animate)
		{
			TicketDetailsPanel.Opacity = 1;
			TicketDetailsPanel.RenderTransform = Transform.Identity;
			return;
		}

		var slide = new TranslateTransform(24, 0);
		TicketDetailsPanel.RenderTransform = slide;
		TicketDetailsPanel.Opacity = 0;
		TicketDetailsPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
		slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(150)));
	}

	private void HideTicketDetails()
	{
		_selectedTicket = null;

		if (TicketDetailsPanel.Visibility != Visibility.Visible)
		{
			return;
		}

		var slide = TicketDetailsPanel.RenderTransform as TranslateTransform ?? new TranslateTransform();
		TicketDetailsPanel.RenderTransform = slide;
		var fadeOut = new DoubleAnimation(TicketDetailsPanel.Opacity, 0, TimeSpan.FromMilliseconds(120));
		fadeOut.Completed += (_, _) => TicketDetailsPanel.Visibility = Visibility.Collapsed;
		TicketDetailsPanel.BeginAnimation(OpacityProperty, fadeOut);
		slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(slide.X, 24, TimeSpan.FromMilliseconds(120)));
	}

	private async void TakeTicketButton_Click(object sender, RoutedEventArgs e)
	{
		if (_selectedTicket is null)
		{
			return;
		}

		SetActionButtonsEnabled(false);

		try
		{
			if (!await _ticketService.TakeTicketAsync(_selectedTicket.Id, _currentAdmin.Id))
			{
				ShowActionMessage("This ticket is no longer available. Refresh the list and choose another one.");
				return;
			}

			await LoadTicketsAsync();
		}
		catch (Exception)
		{
			ShowActionMessage("The ticket could not be taken. Please try again.");
		}
		finally
		{
			SetActionButtonsEnabled(true);
		}
	}

	private async void DeclineTicketButton_Click(object sender, RoutedEventArgs e)
	{
		if (_selectedTicket is null)
		{
			return;
		}

		var reason = DeclineReasonTextBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(reason))
		{
			ShowActionMessage("Enter a reason before declining the ticket.");
			return;
		}

		SetActionButtonsEnabled(false);

		try
		{
			if (!await _ticketService.DeclineTicketAsync(_selectedTicket.Id, _currentAdmin.Id, reason))
			{
				ShowActionMessage("This ticket is no longer available. Refresh the list and choose another one.");
				return;
			}

			HideTicketDetails();
			await LoadTicketsAsync();
		}
		catch (Exception)
		{
			ShowActionMessage("The ticket could not be declined. Please try again.");
		}
		finally
		{
			SetActionButtonsEnabled(true);
		}
	}

	private async void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		await LoadTicketsAsync();
	}

	private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
	{
		HideTicketDetails();
	}

	private void LogoutButton_Click(object sender, RoutedEventArgs e)
	{
		_navigationService.OpenLogin(this);
	}

	private void SetActionButtonsEnabled(bool isEnabled)
	{
		TakeTicketButton.IsEnabled = isEnabled;
		DeclineTicketButton.IsEnabled = isEnabled;
	}

	private void ShowActionMessage(string message)
	{
		TicketActionMessageText.Text = message;
		TicketActionMessageText.Visibility = Visibility.Visible;
	}

	private void HideActionMessage()
	{
		TicketActionMessageText.Visibility = Visibility.Collapsed;
	}

	private static string GetTicketCountText(int count)
	{
		return count == 1 ? "1 ticket" : $"{count} tickets";
	}

	private static string FormatEnum(Enum value)
	{
		return Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1 $2");
	}
}
