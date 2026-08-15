using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HelpDesk_System.Utilities;

public static class DatabaseExceptionClassifier
{
    public static bool IsDatabaseFailure(Exception exception)
    {
        return exception is DbUpdateException or NpgsqlException;
    }
}
