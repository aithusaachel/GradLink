using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GradLink.API.Data;

// SQLite keeps no DateTimeKind, so stored timestamps read back as Unspecified and serialize without a "Z".
// Timestamps are always stored in UTC; reads are marked accordingly.
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
