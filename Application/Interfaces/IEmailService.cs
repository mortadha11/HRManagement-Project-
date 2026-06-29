namespace HRManagement.API.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email to a newly created employee containing their credentials.
    /// </summary>
    Task SendWelcomeEmailAsync(
        string toEmail,
        string toName,
        string username,
        string temporaryPassword);
}
