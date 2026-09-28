using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GradLink.API.Data;

public static class DbUpdateExceptionExtensions
{
    private const int SqliteUniqueConstraintFailed = 2067;

    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintFailed };
}
