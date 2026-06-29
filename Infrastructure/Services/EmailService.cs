using System.Text;
using System.Text.Json;
using HRManagement.API.Application.Interfaces;

namespace HRManagement.API.Infrastructure.Services;

/// <summary>
/// Sends transactional emails via the Brevo (formerly Sendinblue) REST API.
/// No external NuGet package required — uses System.Net.Http.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailService> _logger;

    private const string BrevoApiUrl = "https://api.brevo.com/v3/smtp/email";

    public EmailService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<EmailService> logger)
    {
        _config            = config;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string toName,
        string username,
        string temporaryPassword)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"] ?? "HRManagement";
            var appUrl = _config["Brevo:AppUrl"] ?? "http://localhost:4200";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("Welcome email not sent to {Email}: missing Brevo ApiKey or SenderEmail configuration.", toEmail);
                return;
            }

            var html = BuildHtml(toName, username, temporaryPassword, appUrl);

            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject = "Your HRManagement account is ready",
                htmlContent = html
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("brevo");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            _logger.LogInformation("🚀 [DEBUG] Sending welcome email to {Email} via Brevo API...", toEmail);
            var response = await client.PostAsync(BrevoApiUrl, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Welcome email sent to {Email}", toEmail);
            else
                _logger.LogWarning("Brevo returned {Status}: {Body}", response.StatusCode, body);
        }
        catch (Exception ex)
        {
            // Email failure must never crash the main flow
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
        }
    }

    private static string BuildHtml(
        string name,
        string username,
        string password,
        string appUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>Your HRManagement Account</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f6fb;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6fb;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0"
                       style="background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#4f46e5,#7c3aed);padding:36px 40px;text-align:center;">
                      <h1 style="color:#ffffff;margin:0;font-size:28px;letter-spacing:-0.5px;">HRManagement</h1>
                      <p style="color:#c7d2fe;margin:8px 0 0;font-size:14px;">Human Resources Platform</p>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:40px;">
                      <h2 style="color:#1e1b4b;margin:0 0 8px;">Welcome, {name}! 👋</h2>
                      <p style="color:#6b7280;margin:0 0 28px;line-height:1.6;">
                        Your HR account has been created by the administrator.
                        Use the credentials below to sign in.
                      </p>

                      <!-- Credentials box -->
                      <table width="100%" cellpadding="0" cellspacing="0"
                             style="background:#f0f4ff;border:2px dashed #6366f1;border-radius:10px;margin-bottom:28px;">
                        <tr>
                          <td style="padding:24px 28px;">
                            <p style="margin:0 0 12px;font-size:12px;font-weight:600;
                                      color:#6366f1;text-transform:uppercase;letter-spacing:0.08em;">
                              Your Credentials
                            </p>
                            <table cellpadding="0" cellspacing="0">
                              <tr>
                                <td style="padding:6px 0;color:#6b7280;font-size:14px;width:120px;">Username</td>
                                <td style="padding:6px 0;color:#1e1b4b;font-weight:700;font-size:16px;
                                          font-family:'Courier New',monospace;">{username}</td>
                              </tr>
                              <tr>
                                <td style="padding:6px 0;color:#6b7280;font-size:14px;">Password</td>
                                <td style="padding:6px 0;color:#4f46e5;font-weight:700;font-size:18px;
                                          font-family:'Courier New',monospace;letter-spacing:0.05em;">{password}</td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                      </table>

                      <!-- Warning -->
                      <table width="100%" cellpadding="0" cellspacing="0"
                             style="background:#fffbeb;border-left:4px solid #f59e0b;border-radius:4px;margin-bottom:28px;">
                        <tr>
                          <td style="padding:14px 16px;color:#92400e;font-size:13px;line-height:1.5;">
                            ⚠️ <strong>Please change your password immediately</strong> after your first login.
                            Go to your profile page to update it.
                          </td>
                        </tr>
                      </table>

                      <!-- CTA Button -->
                      <table width="100%" cellpadding="0" cellspacing="0">
                        <tr>
                          <td align="center" style="padding:8px 0 32px;">
                            <a href="{appUrl}/login"
                               style="background:linear-gradient(135deg,#4f46e5,#7c3aed);
                                      color:#ffffff;text-decoration:none;padding:14px 40px;
                                      border-radius:8px;font-weight:600;font-size:15px;
                                      display:inline-block;">
                              Sign In Now →
                            </a>
                          </td>
                        </tr>
                      </table>

                      <p style="color:#9ca3af;font-size:12px;text-align:center;margin:0;">
                        If you did not expect this email, contact your HR administrator.
                      </p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background:#f9fafb;padding:20px 40px;text-align:center;
                               border-top:1px solid #e5e7eb;">
                      <p style="color:#9ca3af;font-size:12px;margin:0;">
                        © {DateTime.UtcNow.Year} HRManagement · This is an automated message, please do not reply.
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
