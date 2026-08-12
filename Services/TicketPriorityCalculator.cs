using HelpDesk_System.Models.Enums;

namespace HelpDesk_System.Services;

public class TicketPriorityCalculator
{
	public TicketPriority Calculate(
		ProblemType problemType,
		WorkImpact workImpact,
		AffectedPeople affectedPeople)
	{
		var priorityScore = GetProblemTypeScore(problemType)
			+ GetWorkImpactScore(workImpact)
			+ GetAffectedPeopleScore(affectedPeople);

		return priorityScore switch
		{
			<= 1 => TicketPriority.VeryLow,
			<= 3 => TicketPriority.Low,
			<= 5 => TicketPriority.Medium,
			<= 7 => TicketPriority.High,
			<= 9 => TicketPriority.VeryHigh,
			_ => TicketPriority.Critical
		};
	}

	private static int GetProblemTypeScore(ProblemType problemType)
	{
		return problemType switch
		{
			ProblemType.AccountsAndAccess => 2,
			ProblemType.NetworkAndInternet => 2,
			ProblemType.Hardware => 1,
			ProblemType.Software => 1,
			ProblemType.TelephonyAndCommunication => 1,
			ProblemType.WorkplaceAndOffice => 1,
			ProblemType.ImprovementSuggestion => 0,
			ProblemType.Other => 0,
			_ => throw new ArgumentOutOfRangeException(nameof(problemType))
		};
	}

	private static int GetWorkImpactScore(WorkImpact workImpact)
	{
		return workImpact switch
		{
			WorkImpact.NotStopped => 0,
			WorkImpact.PartiallyStopped => 3,
			WorkImpact.CompletelyStopped => 6,
			_ => throw new ArgumentOutOfRangeException(nameof(workImpact))
		};
	}

	private static int GetAffectedPeopleScore(AffectedPeople affectedPeople)
	{
		return affectedPeople switch
		{
			AffectedPeople.OnePerson => 0,
			AffectedPeople.GroupOfPeople => 2,
			AffectedPeople.EntireCompany => 4,
			_ => throw new ArgumentOutOfRangeException(nameof(affectedPeople))
		};
	}
}
