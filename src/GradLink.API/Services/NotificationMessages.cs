using GradLink.Shared.Enums;

namespace GradLink.API.Services;

public static class NotificationMessages
{
    public static string NewApplication(string graduateName, string jobTitle) =>
        $"New application from {graduateName} for \"{jobTitle}\"";

    public static string StatusChanged(string jobTitle, ApplicationStatus status) =>
        $"Your application for \"{jobTitle}\" has been updated to: {status}";
}
