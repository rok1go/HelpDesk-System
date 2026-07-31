using HelpDesk_System.Db;
using HelpDesk_System.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_System.Services;

public class AuthService
{
	private readonly IDbContextFactory<HelpDeskDbContext> _contextFactory;

	public AuthService(IDbContextFactory<HelpDeskDbContext> contextFactory)
	{
		_contextFactory = contextFactory;
	}

	public async Task<User?> LoginAsync(string email, string password)
	{
		email = email.Trim().ToLowerInvariant();

		await using var context = await _contextFactory.CreateDbContextAsync();
		var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);

		if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
		{
			return null;
		}

		return user;
	}
}
